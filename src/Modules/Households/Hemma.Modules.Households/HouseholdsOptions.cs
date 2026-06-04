using System.ComponentModel.DataAnnotations;

namespace Hemma.Modules.Households;

public sealed class HouseholdsOptions
{
    [Range(1, 365)]
    public int InvitationLifetimeDays { get; init; } = 14;

    public TimeSpan InvitationLifetime => TimeSpan.FromDays(InvitationLifetimeDays);
}
