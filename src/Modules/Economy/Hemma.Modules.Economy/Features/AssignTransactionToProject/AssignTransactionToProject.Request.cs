namespace Hemma.Modules.Economy.Features.AssignTransactionToProject;

public sealed record AssignTransactionToProjectRequest(Guid HouseholdId, Guid? ProjectId);
