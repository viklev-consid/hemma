using Hemma.Modules.Audit.Gdpr;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Shared.Kernel.Gdpr;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

[NonTransactional]
public sealed class OnUserErasureRequestedHandler(AuditPersonalDataEraser eraser)
{
    public async Task Handle(UserErasureRequestedV1 @event, CancellationToken ct)
    {
        using var activity = AuditTelemetry.ActivitySource.StartActivity(nameof(OnUserErasureRequestedHandler));
        AuditTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(UserErasureRequestedV1)));

        var userRef = new UserRef(@event.UserId, @event.DisplayName);
        await eraser.EraseAsync(userRef, ErasureStrategy.Anonymize, ct);
    }
}
