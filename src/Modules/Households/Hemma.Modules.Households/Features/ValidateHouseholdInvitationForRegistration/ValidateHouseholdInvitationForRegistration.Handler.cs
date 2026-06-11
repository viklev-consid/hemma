using ErrorOr;
using Hemma.Modules.Households.Contracts.Queries;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Households.Features.ValidateHouseholdInvitationForRegistration;

public sealed class ValidateHouseholdInvitationForRegistrationHandler(HouseholdsDbContext db, IClock clock)
{
    public async Task<ErrorOr<ValidateHouseholdInvitationForRegistrationResponse>> Handle(
        ValidateHouseholdInvitationForRegistrationQuery query,
        CancellationToken ct)
    {
        var tokenHash = HouseholdInvitation.HashRawValue(query.InvitationToken);
        var normalizedEmail = query.Email.Trim().ToLowerInvariant();

        var invitation = await db.Invitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

        if (invitation is null ||
            !invitation.CanBeAccepted(clock) ||
            !string.Equals(invitation.Email, normalizedEmail, StringComparison.Ordinal))
        {
            return HouseholdsErrors.InvitationInvalid;
        }

        var householdExists = await db.Households
            .AsNoTracking()
            .AnyAsync(o => o.Id == invitation.HouseholdId && !o.IsDeleted, ct);

        return householdExists
            ? new ValidateHouseholdInvitationForRegistrationResponse(invitation.HouseholdId.Value, invitation.Role.Name)
            : HouseholdsErrors.HouseholdNotFound;
    }
}
