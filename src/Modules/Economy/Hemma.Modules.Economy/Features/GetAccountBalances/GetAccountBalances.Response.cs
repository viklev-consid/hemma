using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.GetAccountBalances;

public sealed record AccountBalanceResponse(Guid AccountId, string Name, string Type, MoneyResponse Balance);

public sealed record GetAccountBalancesResponse(IReadOnlyCollection<AccountBalanceResponse> Accounts);
