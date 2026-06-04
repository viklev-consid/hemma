using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Economy.Gdpr;

public sealed class EconomyPersonalDataEraser : IPersonalDataEraser
{
    public Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct)
    {
        return Task.FromResult(new ErasureResult(user.UserId, strategy, RecordsAffected: 0));
    }
}
