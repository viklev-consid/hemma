using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Organizations.Contracts.Queries;
using Hemma.Modules.Organizations.Domain;
using Hemma.Modules.Organizations.Errors;
using Hemma.Modules.Organizations.Persistence;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Organizations.Features.ValidateOrganizationInvitationForRegistration;

public sealed class ValidateOrganizationInvitationForRegistrationHandler(OrganizationsDbContext db, IClock clock)
{
    public async Task<ErrorOr<ValidateOrganizationInvitationForRegistrationResponse>> Handle(
        ValidateOrganizationInvitationForRegistrationQuery query,
        CancellationToken ct)
    {
        var tokenHash = OrganizationInvitation.HashRawValue(query.InvitationToken);
        var normalizedEmail = query.Email.Trim().ToLowerInvariant();

        var invitation = await db.Invitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

        if (invitation is null ||
            !invitation.CanBeAccepted(clock) ||
            !string.Equals(invitation.Email, normalizedEmail, StringComparison.Ordinal))
        {
            return OrganizationsErrors.InvitationInvalid;
        }

        var organizationExists = await db.Organizations
            .AsNoTracking()
            .AnyAsync(o => o.Id == invitation.OrganizationId && !o.IsDeleted, ct);

        return organizationExists
            ? new ValidateOrganizationInvitationForRegistrationResponse(invitation.OrganizationId.Value, invitation.Role.Name)
            : OrganizationsErrors.OrganizationNotFound;
    }
}
