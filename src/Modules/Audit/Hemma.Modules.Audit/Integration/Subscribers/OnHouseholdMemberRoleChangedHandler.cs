using System.Text.Json;
using Hemma.Modules.Households.Contracts.Events;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

[NonTransactional]
public sealed class OnHouseholdMemberRoleChangedHandler(HouseholdAuditWriter writer)
{
    public async Task Handle(HouseholdMemberRoleChangedV1 @event, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new { @event.HouseholdId, @event.UserId, @event.OldRole, @event.NewRole });
        await writer.WriteAsync(
            "household.member_role_changed",
            @event.ChangedByUserId,
            @event.HouseholdId,
            "HouseholdMember",
            @event.UserId,
            payload,
            @event.EventId,
            ct);
    }
}
