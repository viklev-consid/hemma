using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Organizations.Domain;
using Hemma.Modules.Organizations.Errors;
using Hemma.Modules.Organizations.Persistence;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Organizations.Features.RevokeOrganizationInvitation;

public sealed class RevokeOrganizationInvitationHandler(OrganizationsDbContext db, IClock clock)
{
    public async Task<ErrorOr<Success>> Handle(RevokeOrganizationInvitationCommand cmd, CancellationToken ct)
    {
        var invitation = await db.Invitations
            .FirstOrDefaultAsync(i => i.Id == cmd.InvitationId && i.OrganizationId == cmd.OrganizationId, ct);
        if (invitation is null)
        {
            return OrganizationsErrors.InvitationInvalid;
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
