using Hemma.Modules.Economy.Contracts.Events;
using Wolverine;

namespace Hemma.Modules.Economy.Integration;

public sealed class EconomyAuditPublisher(IMessageBus bus)
{
    public ValueTask PublishAsync(
        Guid householdId,
        string action,
        string resourceType,
        Guid resourceId,
        Guid? actorId,
        CancellationToken ct) =>
        bus.PublishAsync(new EconomyMutationRecordedV1(
            householdId,
            action,
            resourceType,
            resourceId,
            actorId,
            Guid.NewGuid()));
}
