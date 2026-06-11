using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Contracts.Queries;

/// <summary>
/// Cross-module query: returns the linked-transaction spend for each requested Property project.
/// Invoked by the Property module via <c>IMessageBus.InvokeAsync</c> to build the project budget.
/// </summary>
public sealed record GetProjectSpendSummaryQuery(Guid HouseholdId, IReadOnlyList<Guid> ProjectIds);

public sealed record ProjectSpendSummary(Guid ProjectId, MoneyDto LinkedTotal, int TransactionCount);

public sealed record GetProjectSpendSummaryResult(IReadOnlyList<ProjectSpendSummary> Summaries);
