using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.ListRecurringBills;

public sealed record ListRecurringBillsResponse(IReadOnlyCollection<RecurringBillResponse> RecurringBills);
