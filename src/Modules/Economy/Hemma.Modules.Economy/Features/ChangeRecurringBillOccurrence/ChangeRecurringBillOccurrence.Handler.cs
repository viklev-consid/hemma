using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.ChangeRecurringBillOccurrence;

public sealed class ChangeRecurringBillOccurrenceHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<RecurringBillResponse>> Handle(ChangeRecurringBillOccurrenceCommand cmd, CancellationToken ct)
    {
        var billId = new RecurringBillId(cmd.RecurringBillId);
        var bill = await db.RecurringBills
            .Include(x => x.Occurrences)
            .SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == billId, ct);
        if (bill is null)
        {
            return EconomyErrors.RecurringBillNotFound;
        }

        RecurringBillOccurrence? newOccurrence = null;
        var result = cmd.Action switch
        {
            RecurringBillOccurrenceAction.Skip => bill.MarkSkipped(cmd.DueOn).Then(occurrence =>
            {
                newOccurrence = occurrence;
                return Result.Success;
            }),
            RecurringBillOccurrenceAction.Pause => bill.MarkPaused(cmd.DueOn).Then(occurrence =>
            {
                newOccurrence = occurrence;
                return Result.Success;
            }),
            RecurringBillOccurrenceAction.Resume => bill.Resume(cmd.DueOn),
            _ => EconomyErrors.RecurringBillOccurrenceInvalid
        };
        if (result.IsError)
        {
            return result.Errors;
        }

        if (newOccurrence is not null)
        {
            db.Entry(newOccurrence).State = EntityState.Added;
        }

        await db.SaveChangesAsync(ct);
        return RecurringBillResponse.From(bill);
    }
}
