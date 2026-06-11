using ErrorOr;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Households.Features.RevokeHouseholdInvitation;

public sealed class RevokeHouseholdInvitationHandler(HouseholdsDbContext db, IClock clock)
{
    public async Task<ErrorOr<Success>> Handle(RevokeHouseholdInvitationCommand cmd, CancellationToken ct)
    {
        var invitation = await db.Invitations
            .FirstOrDefaultAsync(i => i.Id == cmd.InvitationId && i.HouseholdId == cmd.HouseholdId, ct);
        if (invitation is null)
        {
            return HouseholdsErrors.InvitationInvalid;
        }

        var revoke = invitation.Revoke(cmd.RevokedByUserId, clock);
        if (revoke.IsError)
        {
            return revoke.Errors;
        }

        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}
