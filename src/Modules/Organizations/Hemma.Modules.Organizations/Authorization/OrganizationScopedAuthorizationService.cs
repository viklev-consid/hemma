using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Organizations.Contracts.Authorization;
using Hemma.Modules.Organizations.Domain;
using Hemma.Modules.Organizations.Persistence;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Organizations.Authorization;

internal sealed class OrganizationScopedAuthorizationService(OrganizationsDbContext db)
    : IScopedAuthorizationService<OrganizationScope>
{
    public async Task<ScopedAuthorizationResult> AuthorizeAsync(
        ICurrentUser currentUser,
        OrganizationScope scope,
        string permission,
        ScopedAuthorizationOptions options,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.Id is null)
        {
            return ScopedAuthorizationResult.Denied;
        }

        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return ScopedAuthorizationResult.Denied;
        }

        var organizationId = new OrganizationId(scope.OrganizationId);
        var organizationExists = await db.Organizations
            .AsNoTracking()
            .AnyAsync(o => o.Id == organizationId && !o.IsDeleted, ct);
        if (!organizationExists)
        {
            return ScopedAuthorizationResult.Denied;
        }

        var membership = await db.Memberships
            .AsNoTracking()
            .Where(m => m.OrganizationId == organizationId && m.UserId == userId && m.IsActive)
            .Select(m => new { m.Role })
            .FirstOrDefaultAsync(ct);

        if (membership is not null &&
            OrganizationRolePermissionMap.GetPermissions(membership.Role).Contains(permission, StringComparer.Ordinal))
        {
            return ScopedAuthorizationResult.ScopedPermission;
        }

        if (options.AllowPlatformOverride &&
            currentUser.HasPermission(OrganizationsPermissions.PlatformOverride))
        {
            return ScopedAuthorizationResult.PlatformOverride;
        }

        return ScopedAuthorizationResult.Denied;
    }
}
