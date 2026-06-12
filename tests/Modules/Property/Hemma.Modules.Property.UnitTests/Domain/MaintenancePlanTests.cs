using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class MaintenancePlanTests
{
    [Fact]
    public void Create_WhenTitleIsBlank_ReturnsValidationFailure()
    {
        var result = MaintenancePlan.Create(
            Guid.NewGuid(), " ", null, null, MaintenanceRecurrenceUnit.Month, 6, new DateOnly(2026, 1, 1), 14);

        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public void Create_WhenIntervalOutOfRange_ReturnsValidationFailure(int interval)
    {
        var result = MaintenancePlan.Create(
            Guid.NewGuid(), "Filter", null, null, MaintenanceRecurrenceUnit.Month, interval, new DateOnly(2026, 1, 1), 14);

        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(366)]
    public void Create_WhenLeadTimeOutOfRange_ReturnsValidationFailure(int leadTimeDays)
    {
        var result = MaintenancePlan.Create(
            Guid.NewGuid(), "Filter", null, null, MaintenanceRecurrenceUnit.Month, 6, new DateOnly(2026, 1, 1), leadTimeDays);

        Assert.True(result.IsError);
    }

    [Fact]
    public void NextDueOnOrAfter_WhenAnchorIsInTheFuture_ReturnsAnchor()
    {
        var plan = CreatePlan(MaintenanceRecurrenceUnit.Month, 6, new DateOnly(2026, 9, 1));

        var next = plan.NextDueOnOrAfter(new DateOnly(2026, 6, 11));

        Assert.Equal(new DateOnly(2026, 9, 1), next);
    }

    [Fact]
    public void NextDueOnOrAfter_StepsMonthsForwardToFirstFutureDate()
    {
        // Anchor far in the past, every 6 months: 2024-01 -> 07 -> 2025-01 -> 07 -> 2026-01 -> 07.
        var plan = CreatePlan(MaintenanceRecurrenceUnit.Month, 6, new DateOnly(2024, 1, 15));

        var next = plan.NextDueOnOrAfter(new DateOnly(2026, 6, 11));

        Assert.Equal(new DateOnly(2026, 7, 15), next);
    }

    [Fact]
    public void NextDueOnOrAfter_StepsYearsForwardToFirstFutureDate()
    {
        var plan = CreatePlan(MaintenanceRecurrenceUnit.Year, 1, new DateOnly(2020, 10, 1));

        var next = plan.NextDueOnOrAfter(new DateOnly(2026, 6, 11));

        Assert.Equal(new DateOnly(2026, 10, 1), next);
    }

    [Fact]
    public void NextDueOnOrAfter_WhenFloorEqualsAStep_ReturnsThatStep()
    {
        var plan = CreatePlan(MaintenanceRecurrenceUnit.Month, 3, new DateOnly(2026, 1, 1));

        var next = plan.NextDueOnOrAfter(new DateOnly(2026, 4, 1));

        Assert.Equal(new DateOnly(2026, 4, 1), next);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var plan = CreatePlan(MaintenanceRecurrenceUnit.Month, 6, new DateOnly(2026, 1, 1));

        plan.Deactivate();

        Assert.False(plan.IsActive);
    }

    private static MaintenancePlan CreatePlan(MaintenanceRecurrenceUnit unit, int interval, DateOnly anchor) =>
        MaintenancePlan.Create(Guid.NewGuid(), "Service boiler", null, null, unit, interval, anchor, 14).Value;
}
