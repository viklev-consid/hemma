using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Shared.Infrastructure.Authorization;

public interface IScopedAuthorizationService<TScope>
{
    Task<ScopedAuthorizationResult> AuthorizeAsync(
        ICurrentUser currentUser,
        TScope scope,
        string permission,
        ScopedAuthorizationOptions options,
        CancellationToken ct);
}
