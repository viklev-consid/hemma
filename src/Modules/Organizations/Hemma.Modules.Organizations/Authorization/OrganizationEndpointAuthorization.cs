using Hemma.Modules.Organizations.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Organizations.Authorization;

internal static class OrganizationEndpointAuthorization
{
    public static async Task<ScopedAuthorizationResult> AuthorizeAsync(
        this IScopedAuthorizationService<OrganizationScope> authorization,
        ICurrentUser currentUser,
        OrganizationRef organization,
        string permission,
        ScopedAuthorizationOptions options,
        CancellationToken ct) =>
        await authorization.AuthorizeAsync(
            currentUser,
            new OrganizationScope(organization.Id.Value),
            permission,
            options,
            ct);
}
