using ErrorOr;
using Hemma.Modules.Households.Contracts.Commands;
using Hemma.Modules.Households.Contracts.Events;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hemma.Modules.Households.Features.AcceptHouseholdInvitation;

public sealed class AcceptHouseholdInvitationHandler(HouseholdsDbContext db, IClock clock, IMessageBus bus)
{
    public async Task<ErrorOr<AcceptedHouseholdInvitationForUserResponse>> Handle(AcceptHouseholdInvitationForUserCommand cmd, CancellationToken ct)
    {
        var invitation = await LoadInvitationForTokenAsync(cmd.InvitationToken, ct);
        if (invitation is null)
        {
            return HouseholdsErrors.InvitationInvalid;
        }

        var household = await db.Households
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.Id == invitation.HouseholdId, ct);
        if (household is null || household.IsDeleted)
        {
            return HouseholdsErrors.HouseholdNotFound;
        }

        var accept = invitation.Accept(cmd.UserId, cmd.Email, clock);
        if (accept.IsError)
        {
            return accept.Errors;
        }

        var add = household.AddMember(cmd.UserId, invitation.Role, clock);
        if (add.IsError)
        {
            return add.Errors;
        }

        await db.SaveChangesAsync(ct);
        await bus.PublishAsync(new HouseholdMemberAddedV1(
            household.Id.Value,
            cmd.UserId,
            invitation.Role.Name,
            Guid.NewGuid()));

        return new AcceptedHouseholdInvitationForUserResponse(household.Id.Value, invitation.Role.Name);
    }

    public async Task<ErrorOr<AcceptHouseholdInvitationResponse>> Handle(AcceptHouseholdInvitationCommand cmd, CancellationToken ct)
    {
        var result = await Handle(
            new AcceptHouseholdInvitationForUserCommand(cmd.InvitationToken, cmd.UserId, cmd.Email),
            ct);
        if (result.IsError)
        {
            return result.Errors;
        }

        return new AcceptHouseholdInvitationResponse(result.Value.HouseholdId, result.Value.Role);
    }

    private async Task<HouseholdInvitation?> LoadInvitationForTokenAsync(string rawToken, CancellationToken ct)
    {
        var tokenHash = HouseholdInvitation.HashRawValue(rawToken);
        return await db.Invitations
            .FromSqlInterpolated($"""
                SELECT * FROM households.household_invitations
                WHERE token_hash = {tokenHash}
                FOR UPDATE
                """)
            .FirstOrDefaultAsync(ct);
    }

}
