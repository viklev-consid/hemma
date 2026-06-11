using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.CreateAccount;

public sealed record CreateAccountRequest(Guid HouseholdId, string Name, string Type, MoneyDto OpeningBalance);
