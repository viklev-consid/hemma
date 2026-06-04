using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Households.Gdpr;

public sealed class HouseholdsPersonalDataEraser(HouseholdsDbContext db, IClock clock) : IPersonalDataEraser
{
    public async Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct)
    {
        var recordsAffected = 0;

        var memberships = await db.Memberships
            .Where(m => m.UserId == user.UserId)
            .ToListAsync(ct);

        var activeHouseholdIds = memberships
            .Where(m => m.IsActive)
            .Select(m => m.HouseholdId)
            .Distinct()
            .ToArray();
        var householdsWithActiveMembership = await db.Households
            .Include(o => o.Memberships)
            .Where(o => activeHouseholdIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, ct);

        foreach (var membership in memberships.Where(m => !m.IsActive))
        {
            membership.Anonymize();
            recordsAffected++;
        }

        foreach (var household in householdsWithActiveMembership.Values)
        {
            var membership = household.FindActiveMembership(user.UserId)!;
            var remove = household.RemoveMember(user.UserId, user.UserId, clock);
            if (remove.IsError)
            {
                throw new InvalidOperationException(
                    $"{HouseholdsErrors.OwnedHouseholdsBlockUserErasure.Code}: {remove.FirstError.Description}");
            }

            membership.Anonymize();
            recordsAffected++;
        }

        var invitations = await db.Invitations
            .Where(i => i.InvitedByUserId == user.UserId ||
                i.AcceptedUserId == user.UserId ||
                i.RevokedByUserId == user.UserId)
            .ToListAsync(ct);

        foreach (var invitation in invitations)
        {
            invitation.AnonymizeUserReferences(user.UserId);
            recordsAffected++;
        }

        var deletedHouseholds = await db.Households
            .Where(o => o.DeletedByUserId == user.UserId)
            .ToListAsync(ct);

        foreach (var household in deletedHouseholds)
        {
            household.AnonymizeUserReferences(user.UserId);
            recordsAffected++;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            db.ChangeTracker.Clear();
            throw new InvalidOperationException(
                $"{HouseholdsErrors.OwnedHouseholdsBlockUserErasure.Code}: Household ownership changed concurrently.",
                ex);
        }
        return new ErasureResult(user.UserId, strategy, recordsAffected);
    }
}
