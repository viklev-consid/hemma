using Hemma.Modules.Households.Contracts.Queries;
using Hemma.Modules.Notifications.Contracts.Commands;
using Hemma.Modules.Notifications.Contracts.Dtos;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Hemma.Modules.Property.Jobs;

public sealed partial class MaterializeMaintenanceOccurrencesHandler(
    PropertyDbContext db,
    IMessageBus bus,
    IClock clock,
    ILogger<MaterializeMaintenanceOccurrencesHandler> logger)
{
    public async Task Handle(MaterializeMaintenanceOccurrences command, CancellationToken ct)
    {
        var today = command.Today ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        await HealMissingOccurrencesAsync(today, ct);
        await SendDueRemindersAsync(today, ct);
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

        var added = false;
        foreach (var plan in activePlans)
        {
            if (plansWithUpcoming.Contains(plan.Id))
            {
                continue;
            }

            db.MaintenanceOccurrences.Add(MaintenanceOccurrence.Schedule(plan, plan.NextDueOnOrAfter(today)));
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

    private async Task SendDueRemindersAsync(DateOnly today, CancellationToken ct)
    {
        var upcoming = await db.MaintenanceOccurrences
            .AsNoTracking()
            .Where(o => o.Status == MaintenanceOccurrenceStatus.Upcoming)
            .Join(
                db.MaintenancePlans.AsNoTracking().Where(plan => plan.IsActive),
                o => o.PlanId,
                plan => plan.Id,
                (o, plan) => new DueReminder(o.Id.Value, o.HouseholdId, o.DueDate, plan.Title, plan.LeadTimeDays))
            .ToListAsync(ct);

        var due = upcoming
            .Where(reminder => reminder.DueDate.DayNumber - today.DayNumber <= reminder.LeadTimeDays)
            .ToList();
        if (due.Count == 0)
        {
            return;
        }

        var membersByHousehold = new Dictionary<Guid, IReadOnlyList<HouseholdMemberInfo>>();

        foreach (var reminder in due)
        {
            if (!membersByHousehold.TryGetValue(reminder.HouseholdId, out var members))
            {
                var result = await bus.InvokeAsync<ListHouseholdMembersResult>(
                    new ListHouseholdMembersQuery(reminder.HouseholdId), ct);
                members = result.Members;
                membersByHousehold[reminder.HouseholdId] = members;
            }

            foreach (var member in members)
            {
                await NotifyMemberAsync(reminder, member.UserId, ct);
            }
        }
    }

    private async Task NotifyMemberAsync(DueReminder reminder, Guid userId, CancellationToken ct)
    {
        var idempotencyKey = DeterministicGuid.Create(reminder.OccurrenceId, userId);

        var command = new CreateNotificationCommand(
            userId,
            "property.maintenance.due",
            NotificationCategory.Product,
            NotificationSeverity.Info,
            $"Maintenance due: {reminder.PlanTitle}",
            $"\"{reminder.PlanTitle}\" is due on {reminder.DueDate:yyyy-MM-dd}.",
            new NotificationLinkDto($"/property/maintenance/occurrences/{reminder.OccurrenceId}", "View maintenance"),
            Channels: null,
            idempotencyKey,
            clock.UtcNow);

        try
        {
            await bus.InvokeAsync<ErrorOr.ErrorOr<CreateNotificationResponse>>(command, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogReminderFailed(logger, reminder.OccurrenceId, userId, ex);
        }
    }

    private sealed record DueReminder(Guid OccurrenceId, Guid HouseholdId, DateOnly DueDate, string PlanTitle, int LeadTimeDays);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Duplicate maintenance occurrence skipped during materialisation.")]
    private static partial void LogDuplicateOccurrence(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to send maintenance reminder for occurrence {OccurrenceId} to user {UserId}.")]
    private static partial void LogReminderFailed(ILogger logger, Guid occurrenceId, Guid userId, Exception exception);
}
