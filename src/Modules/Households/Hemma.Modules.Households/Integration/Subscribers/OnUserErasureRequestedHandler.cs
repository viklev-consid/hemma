using Hemma.Modules.Households.Gdpr;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine.Attributes;

namespace Hemma.Modules.Households.Integration.Subscribers;

[NonTransactional]
public sealed class OnUserErasureRequestedHandler(HouseholdsPersonalDataEraser eraser)
{
    public async Task Handle(UserErasureRequestedV1 @event, CancellationToken ct)
    {
        await eraser.EraseAsync(new UserRef(@event.UserId), ErasureStrategy.Anonymize, ct);
    }
}
