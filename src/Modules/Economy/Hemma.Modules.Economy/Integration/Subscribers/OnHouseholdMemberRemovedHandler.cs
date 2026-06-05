using Hemma.Modules.Economy.Gdpr;
using Hemma.Modules.Households.Contracts.Events;
using Wolverine.Attributes;

namespace Hemma.Modules.Economy.Integration.Subscribers;

[NonTransactional]
public sealed class OnHouseholdMemberRemovedHandler(EconomyPersonalDataEraser eraser)
{
    public async Task Handle(HouseholdMemberRemovedV1 @event, CancellationToken ct)
    {
        using var activity = EconomyTelemetry.ActivitySource.StartActivity(nameof(OnHouseholdMemberRemovedHandler));
        EconomyTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(HouseholdMemberRemovedV1)));

        await eraser.EraseHouseholdMemberAsync(@event.HouseholdId, @event.UserId, ct);
    }
}
