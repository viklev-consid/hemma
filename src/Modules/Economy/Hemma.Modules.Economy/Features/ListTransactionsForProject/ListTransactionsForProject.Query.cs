namespace Hemma.Modules.Economy.Features.ListTransactionsForProject;

public sealed record ListTransactionsForProjectQuery(Guid HouseholdId, Guid ProjectId, int Page, int PageSize);
