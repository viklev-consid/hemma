using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Households.Contracts.Events;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Households.Features.RemoveHouseholdMember;

public sealed class RemoveHouseholdMemberHandler(HouseholdsDbContext db, IClock clock, IMessageBus bus)
{
    public async Task<ErrorOr<Success>> Handle(RemoveHouseholdMemberCommand cmd, CancellationToken ct)
    {
        var household = await db.Households
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.Id == cmd.HouseholdId, ct);
        if (household is null)
        {
            return HouseholdsErrors.HouseholdNotFound;
        }

        var remove = household.RemoveMemberAsActor(cmd.RemovedByUserId, cmd.UserId, clock);
        if (remove.IsError)
        {
            return remove.Errors;
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
        await bus.PublishAsync(new HouseholdMemberRemovedV1(
            household.Id.Value,
            cmd.UserId,
            cmd.RemovedByUserId,
            Guid.NewGuid()));
        return Result.Success;
    }
}
