using Hemma.Modules.Economy.Contracts.Events;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Economy.Integration;

public sealed class EconomyAuditPublisher(IMessageBus bus, ICurrentUser currentUser)
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

        await bus.PublishAsync(new EconomyMutationRecordedV1(
            householdId,
            action,
            resourceType,
            resourceId,
            actorId ?? GetCurrentUserId(),
            Guid.NewGuid()));

        EconomyTelemetry.EventsPublished.Add(1, new KeyValuePair<string, object?>("event", nameof(EconomyMutationRecordedV1)));
    }

    private Guid? GetCurrentUserId() =>
        Guid.TryParse(currentUser.Id, out var userId) ? userId : null;
}
