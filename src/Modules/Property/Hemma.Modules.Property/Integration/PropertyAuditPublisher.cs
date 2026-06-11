using Hemma.Modules.Property.Contracts.Events;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Property.Integration;

public sealed class PropertyAuditPublisher(IMessageBus bus, ICurrentUser currentUser)
{
    public async ValueTask PublishAsync(
        Guid householdId,
        string action,
        string resourceType,
        Guid resourceId,
        Guid? actorId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await bus.PublishAsync(new PropertyMutationRecordedV1(
            householdId,
            action,
            resourceType,
            resourceId,
            actorId ?? GetCurrentUserId(),
            Guid.NewGuid()));

        PropertyTelemetry.EventsPublished.Add(1, new KeyValuePair<string, object?>("event", nameof(PropertyMutationRecordedV1)));
    }

    private Guid? GetCurrentUserId() =>
        Guid.TryParse(currentUser.Id, out var userId) ? userId : null;
}
