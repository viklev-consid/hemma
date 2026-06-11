using ErrorOr;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Households.Features.UpdateHousehold;

public sealed class UpdateHouseholdHandler(HouseholdsDbContext db)
{
    public async Task<ErrorOr<UpdateHouseholdResponse>> Handle(UpdateHouseholdCommand cmd, CancellationToken ct)
    {
        var slugResult = HouseholdSlug.Create(cmd.Slug);
        if (slugResult.IsError)
        {
            return slugResult.Errors;
        }

        var slug = slugResult.Value;
        var household = await db.Households.FirstOrDefaultAsync(o => o.Id == cmd.HouseholdId, ct);
        if (household is null || household.IsDeleted)
        {
            return HouseholdsErrors.HouseholdNotFound;
        }

        if (await db.Households.AnyAsync(o => o.Slug == slug && o.Id != cmd.HouseholdId, ct))
        {
            return HouseholdsErrors.SlugAlreadyExists;
        }

        var update = household.Update(cmd.Name, slug);
        if (update.IsError)
        {
            return update.Errors;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            db.ChangeTracker.Clear();
            return HouseholdsErrors.SlugAlreadyExists;
        }

        return new UpdateHouseholdResponse(household.Id.Value, household.Name, household.Slug.Value);
    }
}
