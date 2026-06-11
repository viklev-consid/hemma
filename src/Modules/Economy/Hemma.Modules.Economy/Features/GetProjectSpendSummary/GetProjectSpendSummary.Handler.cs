using Hemma.Modules.Economy.Contracts.Queries;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.GetProjectSpendSummary;

public sealed class GetProjectSpendSummaryHandler(EconomyDbContext db)
{
    private const string currency = "SEK";

    public async Task<GetProjectSpendSummaryResult> Handle(GetProjectSpendSummaryQuery query, CancellationToken ct)
    {
        if (query.ProjectIds.Count == 0)
        {
            return new GetProjectSpendSummaryResult([]);
        }

        var projectIds = query.ProjectIds.Distinct().ToArray();

        var grouped = await db.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.HouseholdId == query.HouseholdId
                && transaction.ProjectId != null
                && projectIds.Contains(transaction.ProjectId.Value))
            .GroupBy(transaction => transaction.ProjectId!.Value)
            .Select(group => new
            {
                ProjectId = group.Key,
                Total = group.Sum(transaction => transaction.Amount.Amount),
                Count = group.Count(),
            })
            .ToListAsync(ct);

        var summaries = grouped
            .Select(row => new ProjectSpendSummary(row.ProjectId, new MoneyDto(row.Total, currency), row.Count))
            .ToArray();

        return new GetProjectSpendSummaryResult(summaries);
    }
}
