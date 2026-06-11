using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.CreateRecurringBill;

public sealed record CreateRecurringBillRequest(
    Guid HouseholdId,
    string Name,
    Guid AccountId,
    Guid? CategoryId,
    MoneyRequest Amount,
    string Type,
    string Direction,
    string CadenceFrequency,
    int CadenceInterval,
    int CadenceDayOfMonth,
    DateOnly StartsOn,
    string? Note);
