namespace Hemma.Modules.Economy.Contracts.Events;

public sealed record BudgetExceededV1(
    Guid HouseholdId,
    Guid BudgetId,
    Guid CategoryId,
    decimal PlannedAmount,
    decimal ActualAmount,
    string Currency,
    Guid EventId);
