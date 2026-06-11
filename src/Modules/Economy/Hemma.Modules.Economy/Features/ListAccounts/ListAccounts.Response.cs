using Hemma.Modules.Economy.Features.CreateAccount;

namespace Hemma.Modules.Economy.Features.ListAccounts;

public sealed record ListAccountsResponse(IReadOnlyCollection<AccountResponse> Accounts);
