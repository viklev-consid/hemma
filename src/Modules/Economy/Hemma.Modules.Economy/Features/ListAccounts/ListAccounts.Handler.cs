using ErrorOr;
using Hemma.Modules.Economy.Features.CreateAccount;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.ListAccounts;

public sealed class ListAccountsHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<ListAccountsResponse>> Handle(ListAccountsQuery query, CancellationToken ct)
    {
        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(account => account.HouseholdId == query.HouseholdId)
            .OrderBy(account => account.Name)
            .ToListAsync(ct);

        return new ListAccountsResponse(accounts.Select(AccountResponse.From).ToArray());
    }
}
