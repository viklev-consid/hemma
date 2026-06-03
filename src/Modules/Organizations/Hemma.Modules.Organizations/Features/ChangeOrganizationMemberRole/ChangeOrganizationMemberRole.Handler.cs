using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Organizations.Contracts.Events;
using Hemma.Modules.Organizations.Domain;
using Hemma.Modules.Organizations.Errors;
using Hemma.Modules.Organizations.Persistence;
using Wolverine;

namespace Hemma.Modules.Organizations.Features.ChangeOrganizationMemberRole;

public sealed class ChangeOrganizationMemberRoleHandler(OrganizationsDbContext db, IMessageBus bus)
{
    public async Task<ErrorOr<ChangeOrganizationMemberRoleResponse>> Handle(ChangeOrganizationMemberRoleCommand cmd, CancellationToken ct)
    {
        var roleResult = OrganizationRole.Create(cmd.Role);
        if (roleResult.IsError)
        {
            return roleResult.Errors;
        }

        var organization = await db.Organizations
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrganizationId, ct);
        if (organization is null)
        {
            return OrganizationsErrors.OrganizationNotFound;
        }

        var change = organization.ChangeMemberRole(cmd.ChangedByUserId, cmd.UserId, roleResult.Value);
        if (change.IsError)
        {
            return change.Errors;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return OrganizationsErrors.ConcurrencyConflict;
        }
        await bus.PublishAsync(new OrganizationMemberRoleChangedV1(
            organization.Id.Value,
            cmd.UserId,
            change.Value,
            roleResult.Value.Name,
            cmd.ChangedByUserId,
            Guid.NewGuid()));

        return new ChangeOrganizationMemberRoleResponse(cmd.UserId, roleResult.Value.Name);
    }
}
