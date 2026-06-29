using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Property.Domain;

public sealed class MaintenanceOccurrence : AggregateRoot<MaintenanceOccurrenceId>
{
    private MaintenanceOccurrence(
        MaintenanceOccurrenceId id,
        MaintenancePlanId planId,
        Guid householdId,
        DateOnly dueDate) : base(id)
    {
        PlanId = planId;
        HouseholdId = householdId;
        DueDate = dueDate;
        OriginalDueDate = dueDate;
        Status = MaintenanceOccurrenceStatus.Upcoming;
    }

    private MaintenanceOccurrence() : base(default!) { }

    public MaintenancePlanId PlanId { get; private set; } = null!;
    public Guid HouseholdId { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DateOnly OriginalDueDate { get; private set; }
    public DateOnly? SnoozedUntil { get; private set; }
    public DateTimeOffset? SnoozedAt { get; private set; }
    public string? SnoozeReason { get; private set; }
    public MaintenanceOccurrenceStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Notes { get; private set; }
    public Guid? SpawnedProjectId { get; private set; }

    public static MaintenanceOccurrence Schedule(MaintenancePlan plan, DateOnly dueDate) =>
        new(MaintenanceOccurrenceId.New(), plan.Id, plan.HouseholdId, dueDate);

    public ErrorOr<Success> Complete(string? notes, IClock clock)
    {
        if (Status != MaintenanceOccurrenceStatus.Upcoming)
        {
            return PropertyErrors.MaintenanceOccurrenceNotOpen;
        }

        Status = MaintenanceOccurrenceStatus.Done;
        CompletedAt = clock.UtcNow;
        Notes = NormalizeNotes(notes);
        return Result.Success;
    }

    public ErrorOr<Success> Skip(string? notes, IClock clock)
    {
        if (Status != MaintenanceOccurrenceStatus.Upcoming)
        {
            return PropertyErrors.MaintenanceOccurrenceNotOpen;
        }

        Status = MaintenanceOccurrenceStatus.Skipped;
        CompletedAt = clock.UtcNow;
        Notes = NormalizeNotes(notes);
        return Result.Success;
    }

    public ErrorOr<Success> PromoteToProject(Guid projectId, IClock clock)
    {
        if (Status != MaintenanceOccurrenceStatus.Upcoming)
        {
            return PropertyErrors.MaintenanceOccurrenceNotOpen;
        }

        Status = MaintenanceOccurrenceStatus.Done;
        CompletedAt = clock.UtcNow;
        SpawnedProjectId = projectId;
        return Result.Success;
    }

    public ErrorOr<Success> Snooze(DateOnly snoozedUntil, string? reason, IClock clock)
    {
        if (Status != MaintenanceOccurrenceStatus.Upcoming)
        {
            return PropertyErrors.MaintenanceOccurrenceNotOpen;
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        if (snoozedUntil <= today)
        {
            return PropertyErrors.MaintenanceOccurrenceSnoozeInvalid;
        }

        SnoozedUntil = snoozedUntil;
        SnoozedAt = clock.UtcNow;
        SnoozeReason = NormalizeNotes(reason);
        return Result.Success;
    }

    public ErrorOr<Success> ClearSnooze()
    {
        if (Status != MaintenanceOccurrenceStatus.Upcoming)
        {
            return PropertyErrors.MaintenanceOccurrenceNotOpen;
        }

        SnoozedUntil = null;
        SnoozedAt = null;
        SnoozeReason = null;
        return Result.Success;
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        return trimmed.Length <= 2000 ? trimmed : trimmed[..2000];
    }
}
