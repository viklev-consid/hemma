using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.GetAccountBalances;

public sealed record AccountBalanceResponse(Guid AccountId, string Name, string Type, MoneyDto Balance);

public sealed record GetAccountBalancesResponse(IReadOnlyCollection<AccountBalanceResponse> Accounts);
