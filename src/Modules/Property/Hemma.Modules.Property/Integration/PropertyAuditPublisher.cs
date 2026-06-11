using Hemma.Modules.Property.Contracts.Events;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Property.Integration;

public sealed class PropertyAuditPublisher(IMessageBus bus, ICurrentUser currentUser)
{
    public ValueTask PublishAsync(
        Guid householdId,
        string action,
        string resourceType,
        Guid resourceId,
        Guid? actorId,
        CancellationToken ct) =>
        bus.PublishAsync(new PropertyMutationRecordedV1(
            householdId,
            action,
            resourceType,
            resourceId,
            actorId ?? GetCurrentUserId(),
            Guid.NewGuid()));

    private Guid? GetCurrentUserId() =>
        Guid.TryParse(currentUser.Id, out var userId) ? userId : null;
}
