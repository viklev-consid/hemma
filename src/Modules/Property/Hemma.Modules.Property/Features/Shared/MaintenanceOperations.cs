using ErrorOr;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Modules.Property.Features.AddAttachment;
using Hemma.Modules.Property.Features.AddLink;
using Hemma.Modules.Property.Features.AddTask;
using Hemma.Modules.Property.Features.ArchiveArea;
using Hemma.Modules.Property.Features.ArchiveTag;
using Hemma.Modules.Property.Features.AssignTags;
using Hemma.Modules.Property.Features.ChangeIssueStatus;
using Hemma.Modules.Property.Features.ChangeProjectStatus;
using Hemma.Modules.Property.Features.ClearOccurrenceSnooze;
using Hemma.Modules.Property.Features.CompleteOccurrence;
using Hemma.Modules.Property.Features.CreateArea;
using Hemma.Modules.Property.Features.CreateHistoryEntry;
using Hemma.Modules.Property.Features.CreateMaintenancePlan;
using Hemma.Modules.Property.Features.CreateProject;
using Hemma.Modules.Property.Features.CreateTag;
using Hemma.Modules.Property.Features.DeactivatePlan;
using Hemma.Modules.Property.Features.DeleteHistoryEntry;
using Hemma.Modules.Property.Features.DeleteIssue;
using Hemma.Modules.Property.Features.DeletePlan;
using Hemma.Modules.Property.Features.DeleteProject;
using Hemma.Modules.Property.Features.DeleteTask;
using Hemma.Modules.Property.Features.GetAttachmentContent;
using Hemma.Modules.Property.Features.GetHistoryPhoto;
using Hemma.Modules.Property.Features.GetIssue;
using Hemma.Modules.Property.Features.GetMaintenancePlan;
using Hemma.Modules.Property.Features.GetProject;
using Hemma.Modules.Property.Features.GetProjectBudget;
using Hemma.Modules.Property.Features.GetProjectTasks;
using Hemma.Modules.Property.Features.LinkIssueToMaintenanceOccurrence;
using Hemma.Modules.Property.Features.LinkIssueToMaintenancePlan;
using Hemma.Modules.Property.Features.ListAreas;
using Hemma.Modules.Property.Features.ListHistory;
using Hemma.Modules.Property.Features.ListIssues;
using Hemma.Modules.Property.Features.ListMaintenancePlans;
using Hemma.Modules.Property.Features.ListProjects;
using Hemma.Modules.Property.Features.ListTags;
using Hemma.Modules.Property.Features.ListUpcomingOccurrences;
using Hemma.Modules.Property.Features.PromoteIssueToProject;
using Hemma.Modules.Property.Features.PromoteOccurrenceToProject;
using Hemma.Modules.Property.Features.RemoveAttachment;
using Hemma.Modules.Property.Features.RemoveLink;
using Hemma.Modules.Property.Features.ReorderAreas;
using Hemma.Modules.Property.Features.ReorderTasks;
using Hemma.Modules.Property.Features.ReportIssue;
using Hemma.Modules.Property.Features.SkipOccurrence;
using Hemma.Modules.Property.Features.SnoozeOccurrence;
using Hemma.Modules.Property.Features.UnlinkIssue;
using Hemma.Modules.Property.Features.UpdateArea;
using Hemma.Modules.Property.Features.UpdateHistoryEntry;
using Hemma.Modules.Property.Features.UpdateIssue;
using Hemma.Modules.Property.Features.UpdateMaintenancePlan;
using Hemma.Modules.Property.Features.UpdateProject;
using Hemma.Modules.Property.Features.UpdateTag;
using Hemma.Modules.Property.Features.UpdateTask;
using Hemma.Modules.Property.Integration;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Contracts;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Features.Shared;

public sealed class MaintenanceOperations(
    PropertyDbContext db,
    PropertyAuditPublisher audit,
    IClock clock,
    ActivityOperations activity)
{
    public async Task<ErrorOr<GetMaintenancePlanResponse>> CreateMaintenancePlanAsync(CreateMaintenancePlanCommand cmd, CancellationToken ct)
    {
        var unit = ParseRecurrenceUnit(cmd.RecurrenceUnit);
        if (unit is null)
        {
            return PropertyErrors.MaintenanceRecurrenceInvalid;
        }

        var areaId = await ValidateAreaAsync(cmd.HouseholdId, cmd.AreaId, ct);
        if (areaId.IsError)
        {
            return areaId.Errors;
        }

        var plan = MaintenancePlan.Create(
            cmd.HouseholdId,
            cmd.Title,
            cmd.Description,
            areaId.Value.Value,
            unit.Value,
            cmd.RecurrenceInterval,
            cmd.AnchorDate,
            cmd.LeadTimeDays);
        if (plan.IsError)
        {
            return plan.Errors;
        }

        db.MaintenancePlans.Add(plan.Value);

        var occurrence = MaintenanceOccurrence.Schedule(plan.Value, plan.Value.NextDueOnOrAfter(Today));
        db.MaintenanceOccurrences.Add(occurrence);

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.maintenance.plan_created", "MaintenancePlan", plan.Value.Id.Value, null, ct);

        return new GetMaintenancePlanResponse(
            await EnrichPlanAsync(MaintenancePlanResponse.FromPlan(plan.Value), includeTags: false, ct),
            MaintenanceOccurrenceResponse.FromOccurrence(occurrence, Today));
    }

    public async Task<ErrorOr<MaintenancePlanResponse>> UpdateMaintenancePlanAsync(UpdateMaintenancePlanCommand cmd, CancellationToken ct)
    {
        var unit = ParseRecurrenceUnit(cmd.RecurrenceUnit);
        if (unit is null)
        {
            return PropertyErrors.MaintenanceRecurrenceInvalid;
        }

        var plan = await LoadPlanAsync(cmd.HouseholdId, cmd.PlanId, ct);
        if (plan is null)
        {
            return PropertyErrors.MaintenancePlanNotFound;
        }

        var areaId = await ValidateAreaAsync(cmd.HouseholdId, cmd.AreaId, ct);
        if (areaId.IsError)
        {
            return areaId.Errors;
        }

        var updated = plan.UpdateDetails(
            cmd.Title,
            cmd.Description,
            areaId.Value.Value,
            unit.Value,
            cmd.RecurrenceInterval,
            cmd.AnchorDate,
            cmd.LeadTimeDays);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.maintenance.plan_updated", "MaintenancePlan", plan.Id.Value, null, ct);
        return await EnrichPlanAsync(MaintenancePlanResponse.FromPlan(plan), includeTags: false, ct);
    }

    public async Task<ErrorOr<MaintenancePlanResponse>> DeactivatePlanAsync(DeactivatePlanCommand cmd, CancellationToken ct)
    {
        var plan = await LoadPlanAsync(cmd.HouseholdId, cmd.PlanId, ct);
        if (plan is null)
        {
            return PropertyErrors.MaintenancePlanNotFound;
        }

        plan.Deactivate();
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.maintenance.plan_deactivated", "MaintenancePlan", plan.Id.Value, null, ct);
        return await EnrichPlanAsync(MaintenancePlanResponse.FromPlan(plan), includeTags: false, ct);
    }

    public async Task<ErrorOr<Deleted>> DeletePlanAsync(DeletePlanCommand cmd, CancellationToken ct)
    {
        var planId = new MaintenancePlanId(cmd.PlanId);
        var plan = await db.MaintenancePlans
            .SingleOrDefaultAsync(p => p.HouseholdId == cmd.HouseholdId && p.Id == planId, ct);
        if (plan is null)
        {
            return PropertyErrors.MaintenancePlanNotFound;
        }

        await db.MaintenanceOccurrences
            .Where(o => o.HouseholdId == cmd.HouseholdId && o.PlanId == planId)
            .ExecuteDeleteAsync(ct);

        db.MaintenancePlans.Remove(plan);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.maintenance.plan_deleted", "MaintenancePlan", cmd.PlanId, null, ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<CompleteOccurrenceResponse>> CompleteOccurrenceAsync(CompleteOccurrenceCommand cmd, CancellationToken ct)
    {
        var occurrence = await LoadOccurrenceAsync(cmd.HouseholdId, cmd.OccurrenceId, ct);
        if (occurrence is null)
        {
            return PropertyErrors.MaintenanceOccurrenceNotFound;
        }

        var completed = occurrence.Complete(cmd.Notes, clock);
        if (completed.IsError)
        {
            return completed.Errors;
        }

        var plan = await LoadPlanAsync(cmd.HouseholdId, occurrence.PlanId.Value, ct);
        var next = await ScheduleNextAsync(plan, occurrence, ct);

        var activityResult = activity.Append(
            cmd.HouseholdId,
            PropertyActivityVerb.MaintenanceCompleted,
            PropertyActivityTargetType.MaintenanceOccurrence,
            occurrence.Id.Value,
            plan is null
                ? "Maintenance occurrence was completed."
                : $"Maintenance \"{plan.Title}\" was completed.",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["planId"] = occurrence.PlanId.Value.ToString(),
                ["dueDate"] = occurrence.DueDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            });
        if (activityResult.IsError)
        {
            return activityResult.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.maintenance.occurrence_completed", "MaintenanceOccurrence", occurrence.Id.Value, null, ct);

        var suggestedAreaName = plan is null
            ? null
            : await PropertyAreaTagEnrichment.AreaNameAsync(db, cmd.HouseholdId, plan.AreaId?.Value, ct);
        var suggested = plan is null
            ? null
            : new SuggestedHistoryEntryResponse(
                DateOnly.FromDateTime(occurrence.CompletedAt!.Value.UtcDateTime),
                plan.Title,
                plan.AreaId?.Value,
                suggestedAreaName,
                null,
                "Maintenance",
                null,
                occurrence.Id.Value,
                []);

        return new CompleteOccurrenceResponse(
            MaintenanceOccurrenceResponse.FromOccurrence(occurrence, Today),
            suggested,
            next is null ? null : MaintenanceOccurrenceResponse.FromOccurrence(next, Today));
    }

    public async Task<ErrorOr<SkipOccurrenceResponse>> SkipOccurrenceAsync(SkipOccurrenceCommand cmd, CancellationToken ct)
    {
        var occurrence = await LoadOccurrenceAsync(cmd.HouseholdId, cmd.OccurrenceId, ct);
        if (occurrence is null)
        {
            return PropertyErrors.MaintenanceOccurrenceNotFound;
        }

        var skipped = occurrence.Skip(cmd.Notes, clock);
        if (skipped.IsError)
        {
            return skipped.Errors;
        }

        var plan = await LoadPlanAsync(cmd.HouseholdId, occurrence.PlanId.Value, ct);
        var next = await ScheduleNextAsync(plan, occurrence, ct);

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.maintenance.occurrence_skipped", "MaintenanceOccurrence", occurrence.Id.Value, null, ct);

        return new SkipOccurrenceResponse(
            MaintenanceOccurrenceResponse.FromOccurrence(occurrence, Today),
            next is null ? null : MaintenanceOccurrenceResponse.FromOccurrence(next, Today));
    }

    public async Task<ErrorOr<PromoteOccurrenceResponse>> PromoteOccurrenceToProjectAsync(PromoteOccurrenceToProjectCommand cmd, CancellationToken ct)
    {
        var status = ParseProjectStatus(cmd.Status);
        if (status is null)
        {
            return PropertyErrors.ProjectStatusInvalid;
        }

        var priority = ParseProjectPriority(cmd.Priority);
        if (priority is null)
        {
            return PropertyErrors.ProjectPriorityInvalid;
        }

        var areaId = await ValidateAreaAsync(cmd.HouseholdId, cmd.AreaId, ct);
        if (areaId.IsError)
        {
            return areaId.Errors;
        }

        var estimate = ToMoney(cmd.BudgetEstimate);
        if (estimate.IsError)
        {
            return estimate.Errors;
        }

        var occurrence = await LoadOccurrenceAsync(cmd.HouseholdId, cmd.OccurrenceId, ct);
        if (occurrence is null)
        {
            return PropertyErrors.MaintenanceOccurrenceNotFound;
        }

        var project = Project.Create(
            cmd.HouseholdId,
            cmd.Name,
            cmd.Description,
            status.Value,
            areaId.Value.Value,
            priority.Value,
            cmd.TargetStartDate,
            cmd.TargetEndDate,
            estimate.Value.Value,
            cmd.Notes);
        if (project.IsError)
        {
            return project.Errors;
        }

        var promoted = occurrence.PromoteToProject(project.Value.Id.Value, clock);
        if (promoted.IsError)
        {
            return promoted.Errors;
        }

        db.Projects.Add(project.Value);

        var plan = await LoadPlanAsync(cmd.HouseholdId, occurrence.PlanId.Value, ct);
        var next = await ScheduleNextAsync(plan, occurrence, ct);

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.created", "Project", project.Value.Id.Value, null, ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.maintenance.occurrence_promoted", "MaintenanceOccurrence", occurrence.Id.Value, null, ct);

        var promotedProjectAreaName = await PropertyAreaTagEnrichment.AreaNameAsync(db, cmd.HouseholdId, project.Value.AreaId?.Value, ct);
        return new PromoteOccurrenceResponse(
            MaintenanceOccurrenceResponse.FromOccurrence(occurrence, Today),
            ProjectResponse.FromProject(project.Value, Today) with { AreaName = promotedProjectAreaName },
            next is null ? null : MaintenanceOccurrenceResponse.FromOccurrence(next, Today));
    }

    public async Task<ErrorOr<MaintenanceOccurrenceResponse>> SnoozeOccurrenceAsync(SnoozeOccurrenceCommand cmd, CancellationToken ct)
    {
        var occurrence = await LoadOccurrenceAsync(cmd.HouseholdId, cmd.OccurrenceId, ct);
        if (occurrence is null)
        {
            return PropertyErrors.MaintenanceOccurrenceNotFound;
        }

        var snoozed = occurrence.Snooze(cmd.SnoozedUntil, cmd.Reason, clock);
        if (snoozed.IsError)
        {
            return snoozed.Errors;
        }

        var activityResult = activity.Append(
            cmd.HouseholdId,
            PropertyActivityVerb.OccurrenceSnoozed,
            PropertyActivityTargetType.MaintenanceOccurrence,
            occurrence.Id.Value,
            $"Maintenance occurrence was snoozed until {occurrence.SnoozedUntil:yyyy-MM-dd}.",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["snoozedUntil"] = occurrence.SnoozedUntil?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["reason"] = occurrence.SnoozeReason
            });
        if (activityResult.IsError)
        {
            return activityResult.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.maintenance.occurrence_snoozed", "MaintenanceOccurrence", occurrence.Id.Value, null, ct);
        return MaintenanceOccurrenceResponse.FromOccurrence(occurrence, Today);
    }

    public async Task<ErrorOr<MaintenanceOccurrenceResponse>> ClearOccurrenceSnoozeAsync(ClearOccurrenceSnoozeCommand cmd, CancellationToken ct)
    {
        var occurrence = await LoadOccurrenceAsync(cmd.HouseholdId, cmd.OccurrenceId, ct);
        if (occurrence is null)
        {
            return PropertyErrors.MaintenanceOccurrenceNotFound;
        }

        var cleared = occurrence.ClearSnooze();
        if (cleared.IsError)
        {
            return cleared.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.maintenance.occurrence_snooze_cleared", "MaintenanceOccurrence", occurrence.Id.Value, null, ct);
        return MaintenanceOccurrenceResponse.FromOccurrence(occurrence, Today);
    }

    public async Task<ErrorOr<GetMaintenancePlanResponse>> GetMaintenancePlanAsync(GetMaintenancePlanQuery query, CancellationToken ct)
    {
        var plan = await LoadPlanAsync(query.HouseholdId, query.PlanId, ct, tracking: false);
        if (plan is null)
        {
            return PropertyErrors.MaintenancePlanNotFound;
        }

        var planId = new MaintenancePlanId(query.PlanId);
        var next = await db.MaintenanceOccurrences
            .AsNoTracking()
            .Where(o => o.HouseholdId == query.HouseholdId && o.PlanId == planId && o.Status == MaintenanceOccurrenceStatus.Upcoming)
            .OrderBy(o => o.DueDate)
            .FirstOrDefaultAsync(ct);

        return new GetMaintenancePlanResponse(
            await EnrichPlanAsync(MaintenancePlanResponse.FromPlan(plan), includeTags: true, ct),
            await EnrichOccurrenceTagsAsync(next is null ? null : MaintenanceOccurrenceResponse.FromOccurrence(next, Today), ct));
    }

    public async Task<ErrorOr<ListMaintenancePlansResponse>> ListMaintenancePlansAsync(ListMaintenancePlansQuery query, CancellationToken ct)
    {
        var plans = db.MaintenancePlans.AsNoTracking().Where(plan => plan.HouseholdId == query.HouseholdId);

        if (query.ActiveOnly == true)
        {
            plans = plans.Where(plan => plan.IsActive);
        }

        if (query.AreaId is not null)
        {
            plans = plans.Where(plan => plan.AreaId == new PropertyAreaId(query.AreaId.Value));
        }

        if (query.TagIds is { Count: > 0 })
        {
            var tagIds = query.TagIds.Distinct().Select(id => new PropertyTagId(id)).ToArray();
            var matchingPlanIds = (await db.TagAssignments
                .AsNoTracking()
                .Where(assignment => assignment.HouseholdId == query.HouseholdId
                    && assignment.TargetType == PropertyTagTargetType.MaintenancePlan
                    && tagIds.Contains(assignment.TagId))
                .GroupBy(assignment => assignment.TargetId)
                .Where(group => group.Select(assignment => assignment.TagId).Distinct().Count() == tagIds.Length)
                .Select(group => group.Key)
                .ToArrayAsync(ct))
                .Select(id => new MaintenancePlanId(id))
                .ToArray();

            plans = plans.Where(plan => matchingPlanIds.Contains(plan.Id));
        }

        var items = await plans
            .OrderByDescending(plan => plan.IsActive)
            .ThenBy(plan => plan.Title)
            .Select(plan => new MaintenancePlanResponse(
                plan.Id.Value,
                plan.HouseholdId,
                plan.Title,
                plan.Description,
                plan.AreaId == null ? null : plan.AreaId.Value.Value,
                null,
                plan.RecurrenceUnit.ToString(),
                plan.RecurrenceInterval,
                plan.AnchorDate,
                plan.LeadTimeDays,
                plan.IsActive))
            .ToArrayAsync(ct);

        var areaNames = await PropertyAreaTagEnrichment.AreaNameMapAsync(db, query.HouseholdId, ct);
        var tagsByPlan = await PropertyAreaTagEnrichment.TagsByTargetAsync(
            db, query.HouseholdId, PropertyTagTargetType.MaintenancePlan, items.Select(plan => plan.PlanId).ToArray(), ct);

        var enriched = items
            .Select(plan => plan with
            {
                AreaName = plan.AreaId is null ? null : areaNames.GetValueOrDefault(plan.AreaId.Value),
                Tags = tagsByPlan.GetValueOrDefault(plan.PlanId, [])
            })
            .ToArray();

        return new ListMaintenancePlansResponse(enriched);
    }

    public async Task<ErrorOr<ListUpcomingOccurrencesResponse>> ListUpcomingOccurrencesAsync(ListUpcomingOccurrencesQuery query, CancellationToken ct)
    {
        var today = Today;
        var horizon = today.AddDays(Math.Max(0, query.HorizonDays));

        var occurrencesQuery = db.MaintenanceOccurrences
            .AsNoTracking()
            .Where(o => o.HouseholdId == query.HouseholdId
                && o.Status == MaintenanceOccurrenceStatus.Upcoming);

        occurrencesQuery = query.IsOverdue switch
        {
            true => occurrencesQuery.Where(o => o.DueDate < today),
            false => occurrencesQuery.Where(o => o.DueDate >= today && (o.SnoozedUntil ?? o.DueDate) <= horizon),
            _ => occurrencesQuery.Where(o => (o.SnoozedUntil ?? o.DueDate) <= horizon),
        };

        var occurrences = await occurrencesQuery
            .OrderBy(o => o.SnoozedUntil ?? o.DueDate)
            .ThenBy(o => o.DueDate)
            .ToListAsync(ct);

        if (occurrences.Count == 0)
        {
            return new ListUpcomingOccurrencesResponse([]);
        }

        var plans = await db.MaintenancePlans
            .AsNoTracking()
            .Where(plan => plan.HouseholdId == query.HouseholdId)
            .ToDictionaryAsync(plan => plan.Id, ct);

        var items = occurrences
            .Select(o =>
            {
                plans.TryGetValue(o.PlanId, out var plan);
                return new UpcomingOccurrenceItem(
                    o.Id.Value,
                    o.PlanId.Value,
                    o.HouseholdId,
                    plan?.Title ?? string.Empty,
                    plan?.AreaId?.Value,
                    null,
                    o.DueDate,
                    o.OriginalDueDate,
                    o.SnoozedUntil ?? o.DueDate,
                    o.SnoozedUntil,
                    o.SnoozedAt,
                    o.SnoozeReason,
                    o.Status.ToString(),
                    OverdueState.ForMaintenanceOccurrence(o, today).IsOverdue,
                    OverdueState.ForMaintenanceOccurrence(o, today).OverdueSince,
                    OverdueState.ForMaintenanceOccurrence(o, today).DaysOverdue);
            })
            .OrderBy(item => item.EffectiveReminderDate)
            .ThenBy(item => item.DueDate)
            .ThenBy(item => item.PlanTitle, StringComparer.Ordinal)
            .ToArray();

        var areaNames = await PropertyAreaTagEnrichment.AreaNameMapAsync(db, query.HouseholdId, ct);
        var tagsByOccurrence = await PropertyAreaTagEnrichment.TagsByTargetAsync(
            db, query.HouseholdId, PropertyTagTargetType.MaintenanceOccurrence, items.Select(item => item.OccurrenceId).ToArray(), ct);

        var enriched = items
            .Select(item => item with
            {
                AreaName = item.AreaId is null ? null : areaNames.GetValueOrDefault(item.AreaId.Value),
                Tags = tagsByOccurrence.GetValueOrDefault(item.OccurrenceId, [])
            })
            .ToArray();

        return new ListUpcomingOccurrencesResponse(enriched);
    }

    private async Task<MaintenancePlanResponse> EnrichPlanAsync(MaintenancePlanResponse response, bool includeTags, CancellationToken ct)
    {
        var areaName = await PropertyAreaTagEnrichment.AreaNameAsync(db, response.HouseholdId, response.AreaId, ct);
        var tags = includeTags
            ? await PropertyAreaTagEnrichment.TagsForTargetAsync(db, response.HouseholdId, PropertyTagTargetType.MaintenancePlan, response.PlanId, ct)
            : response.Tags;
        return response with { AreaName = areaName, Tags = tags };
    }

    private async Task<MaintenanceOccurrenceResponse?> EnrichOccurrenceTagsAsync(MaintenanceOccurrenceResponse? response, CancellationToken ct)
    {
        if (response is null)
        {
            return null;
        }

        var tags = await PropertyAreaTagEnrichment.TagsForTargetAsync(db, response.HouseholdId, PropertyTagTargetType.MaintenanceOccurrence, response.OccurrenceId, ct);
        return response with { Tags = tags };
    }

    private async Task<MaintenanceOccurrence?> ScheduleNextAsync(MaintenancePlan? plan, MaintenanceOccurrence completed, CancellationToken ct)
    {
        if (plan is null || !plan.IsActive)
        {
            return null;
        }

        var floor = Today > completed.DueDate.AddDays(1) ? Today : completed.DueDate.AddDays(1);
        var dueDate = plan.NextDueOnOrAfter(floor);

        // The occurrence being completed is still Upcoming in the database (its Done state is not
        // yet saved), so exclude it when checking whether another Upcoming occurrence already exists.
        var alreadyScheduled = await db.MaintenanceOccurrences
            .AnyAsync(o => o.PlanId == plan.Id && o.Id != completed.Id && o.Status == MaintenanceOccurrenceStatus.Upcoming, ct);
        if (alreadyScheduled)
        {
            return null;
        }

        var next = MaintenanceOccurrence.Schedule(plan, dueDate);
        db.MaintenanceOccurrences.Add(next);
        return next;
    }

    private async Task<MaintenancePlan?> LoadPlanAsync(Guid householdId, Guid planId, CancellationToken ct, bool tracking = true)
    {
        var query = db.MaintenancePlans.Where(plan => plan.HouseholdId == householdId && plan.Id == new MaintenancePlanId(planId));
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(ct);
    }

    private async Task<MaintenanceOccurrence?> LoadOccurrenceAsync(Guid householdId, Guid occurrenceId, CancellationToken ct) =>
        await db.MaintenanceOccurrences
            .SingleOrDefaultAsync(o => o.HouseholdId == householdId && o.Id == new MaintenanceOccurrenceId(occurrenceId), ct);

    private DateOnly Today => DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

    private static MaintenanceRecurrenceUnit? ParseRecurrenceUnit(string unit) =>
        Enum.TryParse<MaintenanceRecurrenceUnit>(unit, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;

    private static ProjectStatus? ParseProjectStatus(string status) =>
        Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;

    private static ProjectPriority? ParseProjectPriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
        {
            return ProjectPriority.Medium;
        }

        return Enum.TryParse<ProjectPriority>(priority, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;
    }

    private static ErrorOr<OptionalMoney> ToMoney(MoneyDto? money)
    {
        if (money is null)
        {
            return new OptionalMoney(null);
        }

        var created = Money.Create(money.Amount, money.Currency);
        return created.IsError ? created.Errors : new OptionalMoney(created.Value);
    }

    private sealed record OptionalMoney(Money? Value);

    private async Task<ErrorOr<OptionalAreaId>> ValidateAreaAsync(Guid householdId, Guid? areaId, CancellationToken ct)
    {
        if (areaId is null)
        {
            return new OptionalAreaId(null);
        }

        var typedId = new PropertyAreaId(areaId.Value);
        var exists = await db.Areas.AnyAsync(area => area.HouseholdId == householdId && area.Id == typedId, ct);
        return exists ? new OptionalAreaId(typedId) : PropertyErrors.AreaNotFound;
    }

    private sealed record OptionalAreaId(PropertyAreaId? Value);
}
