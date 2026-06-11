using System.Text.Json;
using Hemma.Modules.Property.Contracts.Events;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

[NonTransactional]
public sealed class OnPropertyMutationRecordedHandler(HouseholdAuditWriter writer)
{
    public async Task Handle(PropertyMutationRecordedV1 @event, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            @event.HouseholdId,
            @event.Action,
            @event.ResourceType,
            @event.ResourceId,
        });

        await writer.WriteAsync(
            @event.Action,
            @event.ActorId,
            @event.HouseholdId,
            @event.ResourceType,
            @event.ResourceId,
            payload,
            @event.EventId,
            ct);
    }
}
