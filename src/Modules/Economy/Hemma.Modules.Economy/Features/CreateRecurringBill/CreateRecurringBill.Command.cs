namespace Hemma.Modules.Economy.Features.CreateRecurringBill;

public sealed record CreateRecurringBillCommand(
    Guid HouseholdId,
    string Name,
    Guid AccountId,
    Guid? CategoryId,
    decimal Amount,
    string Currency,
    string Type,
    string Direction,
    string CadenceFrequency,
    int CadenceInterval,
    int CadenceDayOfMonth,
    DateOnly StartsOn,
    string? Note);
