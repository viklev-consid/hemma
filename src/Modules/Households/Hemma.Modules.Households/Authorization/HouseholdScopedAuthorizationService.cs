using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Households.Authorization;

internal sealed class HouseholdScopedAuthorizationService(HouseholdsDbContext db)
    : IScopedAuthorizationService<HouseholdScope>
{
    public async Task<ScopedAuthorizationResult> AuthorizeAsync(
        ICurrentUser currentUser,
        HouseholdScope scope,
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

        var householdId = new HouseholdId(scope.HouseholdId);
        var householdExists = await db.Households
            .AsNoTracking()
            .AnyAsync(o => o.Id == householdId && !o.IsDeleted, ct);
        if (!householdExists)
        {
            return ScopedAuthorizationResult.Denied;
        }

        var membership = await db.Memberships
            .AsNoTracking()
            .Where(m => m.HouseholdId == householdId && m.UserId == userId && m.IsActive)
            .Select(m => new { m.Role })
            .FirstOrDefaultAsync(ct);

        if (membership is not null &&
            HouseholdRolePermissionMap.GetPermissions(membership.Role).Contains(permission, StringComparer.Ordinal))
        {
            return ScopedAuthorizationResult.ScopedPermission;
        }

        if (options.AllowPlatformOverride &&
            currentUser.HasPermission(HouseholdsPermissions.PlatformOverride))
        {
            return ScopedAuthorizationResult.PlatformOverride;
        }

        return ScopedAuthorizationResult.Denied;
    }
}
