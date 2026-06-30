using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.ConfirmEstimatedBill;

public sealed record ConfirmEstimatedBillRequest(Guid HouseholdId, Guid OccurrenceId, MoneyDto Amount, DateOnly OccurredOn);
