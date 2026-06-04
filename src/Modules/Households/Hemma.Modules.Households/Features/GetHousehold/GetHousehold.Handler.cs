using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;

namespace Hemma.Modules.Households.Features.GetHousehold;

public sealed class GetHouseholdHandler(HouseholdsDbContext db)
{
    public async Task<ErrorOr<GetHouseholdResponse>> Handle(GetHouseholdQuery query, CancellationToken ct)
    {
        var household = await db.Households
            .AsNoTracking()
            .Where(o => o.Id == query.HouseholdId && !o.IsDeleted)
            .Select(o => new GetHouseholdResponse(o.Id.Value, o.Name, o.Slug.Value, query.AccessMode.ToString()))
            .FirstOrDefaultAsync(ct);

        return household is null ? HouseholdsErrors.HouseholdNotFound : household;
    }
}
