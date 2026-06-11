using ErrorOr;
using Hemma.Modules.Households.Contracts.Events;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hemma.Modules.Households.Features.ChangeHouseholdMemberRole;

public sealed class ChangeHouseholdMemberRoleHandler(HouseholdsDbContext db, IMessageBus bus)
{
    public async Task<ErrorOr<ChangeHouseholdMemberRoleResponse>> Handle(ChangeHouseholdMemberRoleCommand cmd, CancellationToken ct)
    {
        var roleResult = HouseholdRole.Create(cmd.Role);
        if (roleResult.IsError)
        {
            return roleResult.Errors;
        }

        var household = await db.Households
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.Id == cmd.HouseholdId, ct);
        if (household is null)
        {
            return HouseholdsErrors.HouseholdNotFound;
        }

        var change = household.ChangeMemberRole(cmd.ChangedByUserId, cmd.UserId, roleResult.Value);
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
            return HouseholdsErrors.ConcurrencyConflict;
        }
        await bus.PublishAsync(new HouseholdMemberRoleChangedV1(
            household.Id.Value,
            cmd.UserId,
            change.Value,
            roleResult.Value.Name,
            cmd.ChangedByUserId,
            Guid.NewGuid()));

        return new ChangeHouseholdMemberRoleResponse(cmd.UserId, roleResult.Value.Name);
    }
}
