using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.Features.Contracts;

public sealed record RecurringBillOccurrenceResponse(
    DateOnly DueOn,
    string State,
    Guid? TransactionId);

public sealed record RecurringBillResponse(
    Guid RecurringBillId,
    Guid HouseholdId,
    string Name,
    Guid AccountId,
    Guid? CategoryId,
    MoneyResponse Amount,
    string Type,
    string Direction,
    string CadenceFrequency,
    int CadenceInterval,
    int CadenceDayOfMonth,
    DateOnly StartsOn,
    DateOnly NextDueOn,
    string? Note,
    IReadOnlyCollection<RecurringBillOccurrenceResponse> PendingOccurrences)
{
    public static RecurringBillResponse From(RecurringBill bill) =>
        new(
            bill.Id.Value,
            bill.HouseholdId,
            bill.Name,
            bill.AccountId.Value,
            bill.CategoryId?.Value,
            MoneyResponse.From(bill.Amount),
            bill.Type.Name,
            bill.Direction.Name,
            bill.Cadence.Frequency,
            bill.Cadence.Interval,
            bill.Cadence.DayOfMonth,
            bill.StartsOn,
            bill.NextDueOn,
            bill.Note,
            bill.Occurrences
                .Where(x => x.State == RecurringBillOccurrenceState.Pending)
                .OrderBy(x => x.DueOn)
                .Select(x => new RecurringBillOccurrenceResponse(x.DueOn, x.State.Name, x.TransactionId?.Value))
                .ToArray());
}
