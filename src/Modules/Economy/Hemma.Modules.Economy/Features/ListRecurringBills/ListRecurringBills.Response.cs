using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.ListRecurringBills;

public sealed record ListRecurringBillsResponse(IReadOnlyCollection<RecurringBillResponse> RecurringBills);
