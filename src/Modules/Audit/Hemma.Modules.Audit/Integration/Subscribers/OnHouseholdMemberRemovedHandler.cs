using System.Text.Json;
using Hemma.Modules.Households.Contracts.Events;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

[NonTransactional]
public sealed class OnHouseholdMemberRemovedHandler(HouseholdAuditWriter writer)
{
    public async Task Handle(HouseholdMemberRemovedV1 @event, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new { @event.HouseholdId, @event.UserId });
        await writer.WriteAsync(
            "household.member_removed",
            @event.RemovedByUserId,
            @event.HouseholdId,
            "HouseholdMember",
            @event.UserId,
            payload,
            @event.EventId,
            ct);
    }
}
