using ErrorOr;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Modules.Property.Features.Projects;
using Hemma.Modules.Property.Integration;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Contracts;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Features.Maintenance;

public sealed class MaintenanceHandler(
    PropertyDbContext db,
    PropertyAuditPublisher audit,
    IClock clock)
{
    public async Task<ErrorOr<GetMaintenancePlanResponse>> Handle(CreateMaintenancePlanCommand cmd, CancellationToken ct)
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
            MaintenancePlanResponse.FromPlan(plan.Value),
            MaintenanceOccurrenceResponse.FromOccurrence(occurrence));
    }

    public async Task<ErrorOr<MaintenancePlanResponse>> Handle(UpdateMaintenancePlanCommand cmd, CancellationToken ct)
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
        return MaintenancePlanResponse.FromPlan(plan);
    }

    public async Task<ErrorOr<MaintenancePlanResponse>> Handle(DeactivatePlanCommand cmd, CancellationToken ct)
    {
        var plan = await LoadPlanAsync(cmd.HouseholdId, cmd.PlanId, ct);
        if (plan is null)
        {
            return PropertyErrors.MaintenancePlanNotFound;
        }

        plan.Deactivate();
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.maintenance.plan_deactivated", "MaintenancePlan", plan.Id.Value, null, ct);
        return MaintenancePlanResponse.FromPlan(plan);
    }

    public async Task<ErrorOr<Deleted>> Handle(DeletePlanCommand cmd, CancellationToken ct)
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

    public async Task<ErrorOr<CompleteOccurrenceResponse>> Handle(CompleteOccurrenceCommand cmd, CancellationToken ct)
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

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.maintenance.occurrence_completed", "MaintenanceOccurrence", occurrence.Id.Value, null, ct);

        var suggested = plan is null
            ? null
            : new SuggestedHistoryEntryResponse(
                DateOnly.FromDateTime(occurrence.CompletedAt!.Value.UtcDateTime),
                plan.Title,
                plan.AreaId?.Value,
                null,
                null,
                "Maintenance",
                null,
                occurrence.Id.Value,
                []);

        return new CompleteOccurrenceResponse(
            MaintenanceOccurrenceResponse.FromOccurrence(occurrence),
            suggested,
            next is null ? null : MaintenanceOccurrenceResponse.FromOccurrence(next));
    }

    public async Task<ErrorOr<SkipOccurrenceResponse>> Handle(SkipOccurrenceCommand cmd, CancellationToken ct)
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
            MaintenanceOccurrenceResponse.FromOccurrence(occurrence),
            next is null ? null : MaintenanceOccurrenceResponse.FromOccurrence(next));
    }

    public async Task<ErrorOr<PromoteOccurrenceResponse>> Handle(PromoteOccurrenceToProjectCommand cmd, CancellationToken ct)
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

        return new PromoteOccurrenceResponse(
            MaintenanceOccurrenceResponse.FromOccurrence(occurrence),
            ProjectResponse.FromProject(project.Value),
            next is null ? null : MaintenanceOccurrenceResponse.FromOccurrence(next));
    }

    public async Task<ErrorOr<GetMaintenancePlanResponse>> Handle(GetMaintenancePlanQuery query, CancellationToken ct)
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
            MaintenancePlanResponse.FromPlan(plan),
            next is null ? null : MaintenanceOccurrenceResponse.FromOccurrence(next));
    }

    public async Task<ErrorOr<ListMaintenancePlansResponse>> Handle(ListMaintenancePlansQuery query, CancellationToken ct)
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

        return new ListMaintenancePlansResponse(items);
    }

    public async Task<ErrorOr<ListUpcomingOccurrencesResponse>> Handle(ListUpcomingOccurrencesQuery query, CancellationToken ct)
    {
        var horizon = Today.AddDays(Math.Max(0, query.HorizonDays));

        var occurrences = await db.MaintenanceOccurrences
            .AsNoTracking()
            .Where(o => o.HouseholdId == query.HouseholdId
                && o.Status == MaintenanceOccurrenceStatus.Upcoming
                && o.DueDate <= horizon)
            .OrderBy(o => o.DueDate)
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
                    o.Status.ToString());
            })
            .OrderBy(item => item.DueDate)
            .ThenBy(item => item.PlanTitle, StringComparer.Ordinal)
            .ToArray();

        return new ListUpcomingOccurrencesResponse(items);
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
