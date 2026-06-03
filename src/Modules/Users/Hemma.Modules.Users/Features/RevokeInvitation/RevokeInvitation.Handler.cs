using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Users.Domain;
using Hemma.Modules.Users.Errors;
using Hemma.Modules.Users.Persistence;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Users.Features.RevokeInvitation;

public sealed class RevokeInvitationHandler(UsersDbContext db, IClock clock)
{
    public async Task<ErrorOr<RevokeInvitationResponse>> Handle(RevokeInvitationCommand cmd, CancellationToken ct)
        => await UsersTelemetry.InstrumentAsync(nameof(RevokeInvitationHandler), () => HandleCoreAsync(cmd, ct));

    private async Task<ErrorOr<RevokeInvitationResponse>> HandleCoreAsync(RevokeInvitationCommand cmd, CancellationToken ct)
    {
        var invitation = await db.UserInvitations
            .FirstOrDefaultAsync(i => i.Id == new UserInvitationId(cmd.InvitationId), ct);

        if (invitation is null)
        {
            return UsersErrors.UserNotFound;
        }

        var revokeResult = invitation.Revoke(clock);
        if (revokeResult.IsError)
        {
            return revokeResult.Errors;
        }

        await db.SaveChangesAsync(ct);

        return new RevokeInvitationResponse(invitation.Id.Value, invitation.Email, invitation.RevokedAt!.Value);
    }
}
