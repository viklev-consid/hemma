using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Households.Gdpr;

public sealed class HouseholdsPersonalDataExporter(HouseholdsDbContext db) : IPersonalDataExporter
{
    public async Task<PersonalDataExport> ExportAsync(UserRef user, CancellationToken ct)
    {
        var memberships = await db.Memberships
            .AsNoTracking()
            .Where(m => m.UserId == user.UserId)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new
            {
                householdId = m.HouseholdId.Value,
                role = m.Role.Name,
                joinedAt = m.JoinedAt,
                removedAt = m.RemovedAt,
                removedByUserId = m.RemovedByUserId,
                isActive = m.IsActive,
            })
            .ToListAsync(ct);

        var invitations = await db.Invitations
            .AsNoTracking()
            .Where(i => i.InvitedByUserId == user.UserId ||
                i.AcceptedUserId == user.UserId ||
                i.RevokedByUserId == user.UserId)
            .OrderBy(i => i.InvitedAt)
            .Select(i => new
            {
                invitationId = i.Id.Value,
                householdId = i.HouseholdId.Value,
                email = i.Email,
                role = i.Role.Name,
                invitedAt = i.InvitedAt,
                expiresAt = i.ExpiresAt,
                acceptedAt = i.AcceptedAt,
                revokedAt = i.RevokedAt,
                isPending = i.IsPending,
            })
            .ToListAsync(ct);

        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["memberships"] = memberships,
            ["invitations"] = invitations,
        };
        return new PersonalDataExport(user.UserId, "Households", data);
    }
}
