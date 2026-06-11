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
        Status = MaintenanceOccurrenceStatus.Upcoming;
    }

    private MaintenanceOccurrence() : base(default!) { }

    public MaintenancePlanId PlanId { get; private set; } = null!;
    public Guid HouseholdId { get; private set; }
    public DateOnly DueDate { get; private set; }
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
