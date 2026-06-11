namespace Hemma.Modules.Economy.Features.CreateAccount;

public sealed record CreateAccountCommand(Guid HouseholdId, string Name, string Type, decimal OpeningBalanceAmount, string OpeningBalanceCurrency);
