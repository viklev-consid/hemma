using Hemma.Modules.Households.Contracts.Events;
using Hemma.Modules.Property.Gdpr;
using Wolverine.Attributes;

namespace Hemma.Modules.Property.Integration.Subscribers;

[NonTransactional]
public sealed class OnHouseholdDeletedHandler(PropertyPersonalDataEraser eraser)
{
    public async Task Handle(HouseholdDeletedV1 @event, CancellationToken ct)
    {
        using var activity = PropertyTelemetry.ActivitySource.StartActivity(nameof(OnHouseholdDeletedHandler));
        PropertyTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(HouseholdDeletedV1)));

        await eraser.EraseHouseholdAsync(@event.HouseholdId, ct);
    }
}
