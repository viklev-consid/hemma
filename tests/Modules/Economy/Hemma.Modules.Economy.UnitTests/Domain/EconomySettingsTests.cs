using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class EconomySettingsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    public void Create_WhenCycleStartDayIsOutsideSupportedRange_ReturnsError(int cycleStartDay)
    {
        var result = EconomySettings.Create(Guid.NewGuid(), cycleStartDay, "SEK", new DateOnly(2026, 6, 4));

        Assert.True(result.IsError);
    }

    [Fact]
    public void GetPeriodContaining_WhenSettingsCreatedMidCycle_KeepsNormalCycleWindow()
    {
        var settings = EconomySettings.Create(Guid.NewGuid(), 15, "SEK", new DateOnly(2026, 6, 20)).Value;

        var period = settings.GetPeriodContaining(settings.CreatedOn);

        Assert.Equal(new DateOnly(2026, 6, 15), period.StartsOn);
        Assert.Equal(new DateOnly(2026, 7, 14), period.EndsOn);
    }
}
