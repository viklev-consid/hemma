using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.ConfirmEstimatedBill;

public sealed record ConfirmEstimatedBillRequest(Guid HouseholdId, Guid TransactionId, MoneyRequest Amount, DateOnly OccurredOn);
