using Hemma.Modules.Property.Gdpr;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Shared.Kernel.Gdpr;
using Wolverine.Attributes;

namespace Hemma.Modules.Property.Integration.Subscribers;

[NonTransactional]
public sealed class OnUserErasureRequestedHandler(PropertyPersonalDataEraser eraser)
{
    public async Task Handle(UserErasureRequestedV1 @event, CancellationToken ct)
    {
        using var activity = PropertyTelemetry.ActivitySource.StartActivity(nameof(OnUserErasureRequestedHandler));
        PropertyTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(UserErasureRequestedV1)));

        await eraser.EraseAsync(new UserRef(@event.UserId, @event.DisplayName), ErasureStrategy.Anonymize, ct);
    }
}
