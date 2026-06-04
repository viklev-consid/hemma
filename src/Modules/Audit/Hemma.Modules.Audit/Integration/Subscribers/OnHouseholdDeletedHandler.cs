using System.Text.Json;
using Hemma.Modules.Households.Contracts.Events;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

[NonTransactional]
public sealed class OnHouseholdDeletedHandler(HouseholdAuditWriter writer)
{
    public async Task Handle(HouseholdDeletedV1 @event, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new { @event.HouseholdId });
        await writer.WriteAsync(
            "household.deleted",
            @event.DeletedByUserId,
            @event.HouseholdId,
            "Household",
            @event.HouseholdId,
            payload,
            @event.EventId,
            ct);
    }
}
