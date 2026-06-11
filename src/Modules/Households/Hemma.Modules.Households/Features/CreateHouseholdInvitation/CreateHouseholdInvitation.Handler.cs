using ErrorOr;
using Hemma.Modules.Households.Contracts.Events;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wolverine;

namespace Hemma.Modules.Households.Features.CreateHouseholdInvitation;

public sealed class CreateHouseholdInvitationHandler(
    HouseholdsDbContext db,
    IClock clock,
    IMessageBus bus,
    IOptions<HouseholdsOptions> options)
{
    public async Task<ErrorOr<CreateHouseholdInvitationResponse>> Handle(CreateHouseholdInvitationCommand cmd, CancellationToken ct)
    {
        var role = HouseholdRole.Create(cmd.Role);
        if (role.IsError)
        {
            return role.Errors;
        }

        var household = await db.Households
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.Id == cmd.HouseholdId, ct);
        if (household is null || household.IsDeleted)
        {
            return HouseholdsErrors.HouseholdNotFound;
        }

        var roleAllowed = household.EnsureCanInviteRole(cmd.InvitedByUserId, role.Value);
        if (roleAllowed.IsError)
        {
            return roleAllowed.Errors;
        }

        var normalizedEmail = cmd.Email.Trim().ToLowerInvariant();
        // The pre-check gives a fast, readable result; the unique index still owns race protection.
        if (await db.Invitations.AnyAsync(i => i.HouseholdId == cmd.HouseholdId && i.Email == normalizedEmail && i.IsPending, ct))
        {
            return HouseholdsErrors.InvitationInvalid;
        }

        var invitation = HouseholdInvitation.Create(cmd.HouseholdId, normalizedEmail, role.Value, options.Value.InvitationLifetime, cmd.InvitedByUserId, clock);
        if (invitation.IsError)
        {
            return invitation.Errors;
        }

        db.Invitations.Add(invitation.Value.Invitation);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            db.ChangeTracker.Clear();
            return HouseholdsErrors.InvitationInvalid;
        }

        await bus.PublishAsync(new HouseholdInvitationCreatedV1(
            cmd.HouseholdId.Value,
            invitation.Value.Invitation.Id.Value,
            normalizedEmail,
            role.Value.Name,
            invitation.Value.RawToken,
            cmd.InvitedByUserId,
            Guid.NewGuid()));

        return new CreateHouseholdInvitationResponse(
            invitation.Value.Invitation.Id.Value,
            normalizedEmail,
            role.Value.Name,
            invitation.Value.Invitation.ExpiresAt,
            invitation.Value.RawToken);
    }
}
