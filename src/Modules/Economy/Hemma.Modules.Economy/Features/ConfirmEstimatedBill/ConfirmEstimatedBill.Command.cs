namespace Hemma.Modules.Economy.Features.ConfirmEstimatedBill;

public sealed record ConfirmEstimatedBillCommand(Guid HouseholdId, Guid RecurringBillId, Guid OccurrenceId, decimal Amount, string Currency, DateOnly OccurredOn);
