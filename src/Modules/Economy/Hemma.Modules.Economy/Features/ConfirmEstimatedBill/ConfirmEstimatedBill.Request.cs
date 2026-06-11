using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.ConfirmEstimatedBill;

public sealed record ConfirmEstimatedBillRequest(Guid HouseholdId, Guid TransactionId, MoneyDto Amount, DateOnly OccurredOn);
