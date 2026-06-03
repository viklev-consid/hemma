using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.ModuleName.Gdpr;

public sealed class ModuleNamePersonalDataEraser : IPersonalDataEraser
{
    public Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct)
    {
        return Task.FromResult(new ErasureResult(user.UserId, strategy, RecordsAffected: 0));
    }
}
