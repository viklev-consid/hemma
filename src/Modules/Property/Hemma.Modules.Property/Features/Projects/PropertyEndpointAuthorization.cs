using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Hemma.Modules.Property.Features.Projects;

internal static class PropertyEndpointAuthorization
{
    public static async Task<IResult?> AuthorizeHouseholdAsync(
        Guid householdId,
        string permission,
        IScopedAuthorizationService<HouseholdScope> authorization,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var access = await authorization.AuthorizeAsync(
            currentUser,
            new HouseholdScope(householdId),
            permission,
            ScopedAuthorizationOptions.WithPlatformOverride,
            ct);

        return access.Succeeded ? null : Results.Forbid();
    }
}
