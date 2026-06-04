using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Households.Authorization;

internal static class HouseholdEndpointAuthorization
{
    public static async Task<ScopedAuthorizationResult> AuthorizeAsync(
        this IScopedAuthorizationService<HouseholdScope> authorization,
        ICurrentUser currentUser,
        HouseholdRef household,
        string permission,
        ScopedAuthorizationOptions options,
        CancellationToken ct) =>
        await authorization.AuthorizeAsync(
            currentUser,
            new HouseholdScope(household.Id.Value),
            permission,
            options,
            ct);
}
