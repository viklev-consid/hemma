using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.CreateRecurringBill;

public sealed record CreateRecurringBillRequest(
    Guid HouseholdId,
    string Name,
    Guid AccountId,
    Guid? CategoryId,
    MoneyDto Amount,
    string Type,
    string Direction,
    string CadenceFrequency,
    int CadenceInterval,
    int CadenceDayOfMonth,
    DateOnly StartsOn,
    string? Note);
