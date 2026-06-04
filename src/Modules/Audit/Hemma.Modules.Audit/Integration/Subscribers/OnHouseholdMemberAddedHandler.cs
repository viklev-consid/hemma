using System.Text.Json;
using Hemma.Modules.Households.Contracts.Events;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

[NonTransactional]
public sealed class OnHouseholdMemberAddedHandler(HouseholdAuditWriter writer)
{
    public async Task Handle(HouseholdMemberAddedV1 @event, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new { @event.HouseholdId, @event.UserId, @event.Role });
        await writer.WriteAsync(
            "household.member_added",
            @event.UserId,
            @event.HouseholdId,
            "HouseholdMember",
            @event.UserId,
            payload,
            @event.EventId,
            ct);
    }
}
