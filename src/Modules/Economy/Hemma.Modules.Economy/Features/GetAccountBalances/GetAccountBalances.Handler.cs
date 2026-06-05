using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.GetAccountBalances;

public sealed class GetAccountBalancesHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<GetAccountBalancesResponse>> Handle(GetAccountBalancesQuery query, CancellationToken ct)
    {
        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(account => account.HouseholdId == query.HouseholdId)
            .OrderBy(account => account.Name)
            .ToListAsync(ct);

        var transactions = await db.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.HouseholdId == query.HouseholdId)
            .ToListAsync(ct);

        var balances = accounts.Select(account =>
        {
            var delta = transactions
                .Where(transaction => transaction.AccountId == account.Id)
                .Sum(GetSignedAmount);
            var balance = Money.Create(account.OpeningBalance.Amount + delta, account.OpeningBalance.Currency).Value;
            return new AccountBalanceResponse(account.Id.Value, account.Name, account.Type.Name, MoneyResponse.From(balance));
        }).ToArray();

        return new GetAccountBalancesResponse(balances);
    }

    private static decimal GetSignedAmount(Transaction transaction)
    {
        if (transaction.Kind == TransactionKind.Income)
        {
            return transaction.Amount.Amount;
        }

        if (transaction.Kind == TransactionKind.Transfer && !transaction.IsTransferOutflow)
        {
            return transaction.Amount.Amount;
        }

        return -transaction.Amount.Amount;
    }
}
