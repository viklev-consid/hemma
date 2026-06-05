namespace Hemma.Modules.Economy.Features.ConfirmEstimatedBill;

public sealed record ConfirmEstimatedBillCommand(Guid HouseholdId, Guid RecurringBillId, Guid TransactionId, decimal Amount, string Currency, DateOnly OccurredOn);
