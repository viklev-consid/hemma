using System.Text.Json;
using Hemma.Modules.Households.Contracts.Events;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

[NonTransactional]
public sealed class OnHouseholdCreatedHandler(HouseholdAuditWriter writer)
{
    public async Task Handle(HouseholdCreatedV1 @event, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new { @event.HouseholdId });
        await writer.WriteAsync(
            "household.created",
            @event.CreatedByUserId,
            @event.HouseholdId,
            "Household",
            @event.HouseholdId,
            payload,
            @event.EventId,
            ct);
    }
}
