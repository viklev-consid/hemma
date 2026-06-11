using Hemma.Modules.Economy.Gdpr;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Shared.Kernel.Gdpr;
using Wolverine.Attributes;

namespace Hemma.Modules.Economy.Integration.Subscribers;

[NonTransactional]
public sealed class OnUserErasureRequestedHandler(EconomyPersonalDataEraser eraser)
{
    public async Task Handle(UserErasureRequestedV1 @event, CancellationToken ct)
    {
        using var activity = EconomyTelemetry.ActivitySource.StartActivity(nameof(OnUserErasureRequestedHandler));
        EconomyTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(UserErasureRequestedV1)));

        await eraser.EraseAsync(new UserRef(@event.UserId, @event.DisplayName), ErasureStrategy.Anonymize, ct);
    }
}
