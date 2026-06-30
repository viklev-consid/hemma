namespace Hemma.Modules.Economy.Features.DeleteTransaction;

public sealed record DeleteTransactionCommand(Guid HouseholdId, Guid TransactionId);
