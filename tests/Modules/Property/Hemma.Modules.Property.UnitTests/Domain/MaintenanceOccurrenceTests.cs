using Hemma.Modules.Property.Domain;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Property.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class MaintenanceOccurrenceTests
{
    private static readonly DateTimeOffset now = new(2026, 6, 11, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Schedule_CreatesUpcomingOccurrenceForPlanHousehold()
    {
        var plan = CreatePlan();

        var occurrence = MaintenanceOccurrence.Schedule(plan, new DateOnly(2026, 7, 1));

        Assert.Equal(MaintenanceOccurrenceStatus.Upcoming, occurrence.Status);
        Assert.Equal(plan.Id, occurrence.PlanId);
        Assert.Equal(plan.HouseholdId, occurrence.HouseholdId);
        Assert.Equal(new DateOnly(2026, 7, 1), occurrence.DueDate);
    }

    [Fact]
    public void Complete_SetsDoneAndCompletedAt()
    {
        var occurrence = MaintenanceOccurrence.Schedule(CreatePlan(), new DateOnly(2026, 7, 1));

        var result = occurrence.Complete("Replaced filter", new FixedClock(now));

        Assert.False(result.IsError);
        Assert.Equal(MaintenanceOccurrenceStatus.Done, occurrence.Status);
        Assert.Equal(now, occurrence.CompletedAt);
        Assert.Equal("Replaced filter", occurrence.Notes);
    }

    [Fact]
    public void Skip_SetsSkipped()
    {
        var occurrence = MaintenanceOccurrence.Schedule(CreatePlan(), new DateOnly(2026, 7, 1));

        var result = occurrence.Skip(null, new FixedClock(now));

        Assert.False(result.IsError);
        Assert.Equal(MaintenanceOccurrenceStatus.Skipped, occurrence.Status);
    }

    [Fact]
    public void PromoteToProject_SetsDoneAndSpawnedProjectId()
    {
        var occurrence = MaintenanceOccurrence.Schedule(CreatePlan(), new DateOnly(2026, 7, 1));
        var projectId = Guid.NewGuid();

        var result = occurrence.PromoteToProject(projectId, new FixedClock(now));

        Assert.False(result.IsError);
        Assert.Equal(MaintenanceOccurrenceStatus.Done, occurrence.Status);
        Assert.Equal(projectId, occurrence.SpawnedProjectId);
    }

    [Fact]
    public void Complete_WhenAlreadyDone_ReturnsValidationFailure()
    {
        var occurrence = MaintenanceOccurrence.Schedule(CreatePlan(), new DateOnly(2026, 7, 1));
        occurrence.Complete(null, new FixedClock(now));

        var result = occurrence.Complete(null, new FixedClock(now));

        Assert.True(result.IsError);
    }

    private static MaintenancePlan CreatePlan() =>
        MaintenancePlan.Create(Guid.NewGuid(), "Service boiler", null, null, MaintenanceRecurrenceUnit.Month, 6, new DateOnly(2026, 1, 1), 14).Value;

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
