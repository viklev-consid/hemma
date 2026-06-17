using Hemma.Modules.Notifications.Contracts.Dtos;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Gdpr;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hemma.Modules.Property.Jobs;

public sealed partial class MaterializeMaintenanceOccurrencesHandler(
    PropertyDbContext db,
    IClock clock,
    PropertyNotificationDispatcher notifications,
    PropertyPersonalDataEraser eraser,
    ILogger<MaterializeMaintenanceOccurrencesHandler> logger)
{
    private const int workApproachingWindowDays = 7;

    public async Task Handle(MaterializeMaintenanceOccurrences command, CancellationToken ct)
    {
        var today = command.Today ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        await HealMissingOccurrencesAsync(today, ct);
        await eraser.ProcessPendingBlobDeletionsAsync(ct);
        await SendPropertyNotificationsAsync(today, ct);
    }

    private async Task HealMissingOccurrencesAsync(DateOnly today, CancellationToken ct)
    {
        var activePlans = await db.MaintenancePlans.Where(plan => plan.IsActive).ToListAsync(ct);
        if (activePlans.Count == 0)
        {
            return;
        }

        var plansWithUpcoming = (await db.MaintenanceOccurrences
            .Where(o => o.Status == MaintenanceOccurrenceStatus.Upcoming)
            .Select(o => o.PlanId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        // The latest due date (any status) per plan. We must schedule the next occurrence strictly
        // after it: a terminal (Done/Skipped) occurrence sitting at the computed next-due would
        // otherwise collide with the unique (plan_id, due_date) index and get silently swallowed
        // below, permanently stalling materialisation. This mirrors ScheduleNextAsync's floor.
        var latestDueByPlan = (await db.MaintenanceOccurrences
            .Select(o => new { o.PlanId, o.DueDate })
            .ToListAsync(ct))
            .GroupBy(o => o.PlanId)
            .ToDictionary(group => group.Key, group => group.Max(o => o.DueDate));

        var added = false;
        foreach (var plan in activePlans)
        {
            if (plansWithUpcoming.Contains(plan.Id))
            {
                continue;
            }

            if (!plan.HasValidRecurrenceForScheduling)
            {
                LogInvalidRecurrence(logger, plan.Id.Value, plan.HouseholdId, plan.RecurrenceUnit.ToString(), plan.RecurrenceInterval);
            }

            var floor = today;
            if (latestDueByPlan.TryGetValue(plan.Id, out var latestDue) && latestDue >= today)
            {
                floor = latestDue.AddDays(1);
            }

            db.MaintenanceOccurrences.Add(MaintenanceOccurrence.Schedule(plan, plan.NextDueOnOrAfter(floor)));
            added = true;
        }

        if (!added)
        {
            return;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // A concurrent run already materialised one of these occurrences; the unique
            // (plan_id, due_date) index protected us. Drop the batch and let the next run heal.
            LogDuplicateOccurrence(logger, ex);
            db.ChangeTracker.Clear();
        }
    }

    private async Task SendPropertyNotificationsAsync(DateOnly today, CancellationToken ct)
    {
        var pending = new List<PropertyNotification>();
        pending.AddRange(await GetMaintenanceNotificationsAsync(today, ct));
        pending.AddRange(await GetProjectNotificationsAsync(today, ct));
        pending.AddRange(await GetTaskNotificationsAsync(today, ct));
        pending.AddRange(await GetIssueDueNotificationsAsync(today, ct));

        foreach (var notification in pending)
        {
            await notifications.NotifyHouseholdAsync(notification, ct);
        }
    }

    private async Task<IReadOnlyList<PropertyNotification>> GetMaintenanceNotificationsAsync(DateOnly today, CancellationToken ct)
    {
        var upcoming = await db.MaintenanceOccurrences
            .AsNoTracking()
            .Where(o => o.Status == MaintenanceOccurrenceStatus.Upcoming)
            .Join(
                db.MaintenancePlans.AsNoTracking().Where(plan => plan.IsActive),
                o => o.PlanId,
                plan => plan.Id,
                (o, plan) => new MaintenanceReminder(
                    o.Id.Value,
                    o.HouseholdId,
                    o.DueDate,
                    o.SnoozedUntil,
                    plan.Title,
                    plan.LeadTimeDays))
            .ToListAsync(ct);

        var result = new List<PropertyNotification>();
        foreach (var reminder in upcoming)
        {
            var scheduledReminderDate = reminder.DueDate.AddDays(-reminder.LeadTimeDays);
            var effectiveReminderDate = reminder.SnoozedUntil ?? scheduledReminderDate;
            // This remains true throughout the lead window. The dispatcher uses RelevantDate
            // in the idempotency key, so the same reminder is de-duped across daily job runs.
            if (effectiveReminderDate <= today && reminder.DueDate >= today)
            {
                result.Add(new PropertyNotification(
                    "MaintenanceOccurrence",
                    reminder.OccurrenceId,
                    reminder.HouseholdId,
                    "due_soon",
                    effectiveReminderDate,
                    "property.maintenance.due",
                    NotificationSeverity.Info,
                    $"Maintenance due: {reminder.PlanTitle}",
                    $"\"{reminder.PlanTitle}\" is due on {reminder.DueDate:yyyy-MM-dd}.",
                    new NotificationLinkDto($"/property/maintenance/occurrences/{reminder.OccurrenceId}", "View maintenance")));
            }

            if (reminder.SnoozedUntil == today)
            {
                result.Add(new PropertyNotification(
                    "MaintenanceOccurrence",
                    reminder.OccurrenceId,
                    reminder.HouseholdId,
                    "snooze_due",
                    today,
                    "property.maintenance.snooze_due",
                    NotificationSeverity.Warning,
                    $"Snoozed maintenance is ready: {reminder.PlanTitle}",
                    $"\"{reminder.PlanTitle}\" was snoozed until today.",
                    new NotificationLinkDto($"/property/maintenance/occurrences/{reminder.OccurrenceId}", "View maintenance")));
            }

            if (reminder.DueDate < today && (reminder.SnoozedUntil is null || reminder.SnoozedUntil.Value <= today))
            {
                result.Add(new PropertyNotification(
                    "MaintenanceOccurrence",
                    reminder.OccurrenceId,
                    reminder.HouseholdId,
                    "overdue",
                    today,
                    "property.maintenance.overdue",
                    NotificationSeverity.Warning,
                    $"Maintenance overdue: {reminder.PlanTitle}",
                    $"\"{reminder.PlanTitle}\" was due on {reminder.DueDate:yyyy-MM-dd}.",
                    new NotificationLinkDto($"/property/maintenance/occurrences/{reminder.OccurrenceId}", "View maintenance")));
            }
        }

        return result;
    }

    private async Task<IReadOnlyList<PropertyNotification>> GetProjectNotificationsAsync(DateOnly today, CancellationToken ct)
    {
        var projects = await db.Projects
            .AsNoTracking()
            .Where(project => project.Status != ProjectStatus.Done && project.TargetEndDate != null && project.TargetEndDate <= today.AddDays(workApproachingWindowDays))
            .Select(project => new WorkReminder(
                project.Id.Value,
                project.HouseholdId,
                project.Name,
                project.TargetEndDate!.Value))
            .ToListAsync(ct);

        return projects
            .Select(project =>
            {
                var overdue = project.RelevantDate < today;
                return new PropertyNotification(
                    "Project",
                    project.SourceId,
                    project.HouseholdId,
                    overdue ? "overdue" : "due_soon",
                    overdue ? today : project.RelevantDate,
                    overdue ? "property.project.overdue" : "property.project.due",
                    overdue ? NotificationSeverity.Warning : NotificationSeverity.Info,
                    overdue ? $"Project overdue: {project.Title}" : $"Project due soon: {project.Title}",
                    overdue
                        ? $"\"{project.Title}\" was due on {project.RelevantDate:yyyy-MM-dd}."
                        : $"\"{project.Title}\" is due on {project.RelevantDate:yyyy-MM-dd}.",
                    new NotificationLinkDto($"/property/projects/{project.SourceId}", "View project"));
            })
            .ToArray();
    }

    private async Task<IReadOnlyList<PropertyNotification>> GetTaskNotificationsAsync(DateOnly today, CancellationToken ct)
    {
        var tasks = await db.Projects
            .AsNoTracking()
            .Where(project => project.Status != ProjectStatus.Done)
            .SelectMany(
                project => project.Tasks
                    .Where(task => task.Status != ProjectTaskStatus.Done && task.DueDate != null && task.DueDate <= today.AddDays(workApproachingWindowDays))
                    .Select(task => new TaskReminder(
                        task.Id.Value,
                        project.HouseholdId,
                        project.Id.Value,
                        task.Title,
                        task.DueDate!.Value)))
            .ToListAsync(ct);

        return tasks
            .Select(task =>
            {
                var overdue = task.RelevantDate < today;
                return new PropertyNotification(
                    "ProjectTask",
                    task.SourceId,
                    task.HouseholdId,
                    overdue ? "overdue" : "due_soon",
                    overdue ? today : task.RelevantDate,
                    overdue ? "property.project_task.overdue" : "property.project_task.due",
                    overdue ? NotificationSeverity.Warning : NotificationSeverity.Info,
                    overdue ? $"Task overdue: {task.Title}" : $"Task due soon: {task.Title}",
                    overdue
                        ? $"\"{task.Title}\" was due on {task.RelevantDate:yyyy-MM-dd}."
                        : $"\"{task.Title}\" is due on {task.RelevantDate:yyyy-MM-dd}.",
                    new NotificationLinkDto($"/property/projects/{task.ProjectId}/tasks/{task.SourceId}", "View task"));
            })
            .ToArray();
    }

    private async Task<IReadOnlyList<PropertyNotification>> GetIssueDueNotificationsAsync(DateOnly today, CancellationToken ct)
    {
        var issues = await db.Issues
            .AsNoTracking()
            .Where(issue => (issue.Status == PropertyIssueStatus.Open || issue.Status == PropertyIssueStatus.InProgress)
                && issue.DueDate != null
                && issue.DueDate <= today.AddDays(workApproachingWindowDays))
            .Select(issue => new WorkReminder(
                issue.Id.Value,
                issue.HouseholdId,
                issue.Title,
                issue.DueDate!.Value))
            .ToListAsync(ct);

        return issues
            .Select(issue =>
            {
                var overdue = issue.RelevantDate < today;
                return new PropertyNotification(
                    "PropertyIssue",
                    issue.SourceId,
                    issue.HouseholdId,
                    overdue ? "overdue" : "due_soon",
                    overdue ? today : issue.RelevantDate,
                    overdue ? "property.issue.overdue" : "property.issue.due",
                    overdue ? NotificationSeverity.Warning : NotificationSeverity.Info,
                    overdue ? $"Issue overdue: {issue.Title}" : $"Issue due soon: {issue.Title}",
                    overdue
                        ? $"\"{issue.Title}\" was due on {issue.RelevantDate:yyyy-MM-dd}."
                        : $"\"{issue.Title}\" is due on {issue.RelevantDate:yyyy-MM-dd}.",
                    new NotificationLinkDto($"/property/issues/{issue.SourceId}", "View issue"));
            })
            .ToArray();
    }

    private sealed record MaintenanceReminder(Guid OccurrenceId, Guid HouseholdId, DateOnly DueDate, DateOnly? SnoozedUntil, string PlanTitle, int LeadTimeDays);

    private sealed record WorkReminder(Guid SourceId, Guid HouseholdId, string Title, DateOnly RelevantDate);

    private sealed record TaskReminder(Guid SourceId, Guid HouseholdId, Guid ProjectId, string Title, DateOnly RelevantDate);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Duplicate maintenance occurrence skipped during materialisation.")]
    private static partial void LogDuplicateOccurrence(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Maintenance plan {PlanId} in household {HouseholdId} has invalid persisted recurrence {RecurrenceUnit}/{RecurrenceInterval}; scheduling defensively from today.")]
    private static partial void LogInvalidRecurrence(ILogger logger, Guid planId, Guid householdId, string recurrenceUnit, int recurrenceInterval);
}
