using Hemma.Shared.Kernel.Gdpr;

namespace Hemma.Shared.Kernel.Interfaces;

public interface IPersonalDataEraser
{
    Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct);
}
