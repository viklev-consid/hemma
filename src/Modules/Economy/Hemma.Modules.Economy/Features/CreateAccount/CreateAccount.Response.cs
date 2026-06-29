using Hemma.Modules.Economy.Domain;
using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.CreateAccount;

public sealed record AccountResponse(Guid AccountId, Guid HouseholdId, string Name, string Type, MoneyDto OpeningBalance)
{
    public static AccountResponse From(Account account) =>
        new(account.Id.Value, account.HouseholdId, account.Name, account.Type.Name, MoneyContract.From(account.OpeningBalance));
}
