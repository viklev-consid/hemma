using ErrorOr;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.ListRecurringBills;

public sealed class ListRecurringBillsHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<ListRecurringBillsResponse>> Handle(ListRecurringBillsQuery query, CancellationToken ct)
    {
        var bills = await db.RecurringBills
            .AsNoTracking()
            .Include(x => x.Occurrences)
            .Where(x => x.HouseholdId == query.HouseholdId)
            .OrderBy(x => x.NextDueOn)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return new ListRecurringBillsResponse(bills.Select(RecurringBillResponse.From).ToArray());
    }
}
