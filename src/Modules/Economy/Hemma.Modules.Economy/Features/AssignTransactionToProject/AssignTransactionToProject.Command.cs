namespace Hemma.Modules.Economy.Features.AssignTransactionToProject;

public sealed record AssignTransactionToProjectCommand(Guid HouseholdId, Guid TransactionId, Guid? ProjectId);
