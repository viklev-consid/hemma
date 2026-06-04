using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Households.Contracts.Events;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Households.Features.DeleteHousehold;

public sealed class DeleteHouseholdHandler(HouseholdsDbContext db, IClock clock, IMessageBus bus)
{
    public async Task<ErrorOr<Success>> Handle(DeleteHouseholdCommand cmd, CancellationToken ct)
    {
        var household = await db.Households
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.Id == cmd.HouseholdId, ct);

        if (household is null)
        {
            return HouseholdsErrors.HouseholdNotFound;
        }

        var delete = household.Delete(cmd.DeletedByUserId, clock);
        if (delete.IsError)
        {
            return delete.Errors;
        }

        await db.SaveChangesAsync(ct);
        await bus.PublishAsync(new HouseholdDeletedV1(cmd.HouseholdId.Value, cmd.DeletedByUserId, Guid.NewGuid()));
        return Result.Success;
    }
}
