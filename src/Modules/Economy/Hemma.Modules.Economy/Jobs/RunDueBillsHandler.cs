using Hemma.Modules.Economy.Contracts.Events;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hemma.Modules.Economy.Jobs;

public sealed class RunDueBillsHandler(EconomyDbContext db, IClock clock, IMessageBus bus)
{
    public async Task Handle(RunDueBills command, CancellationToken ct)
    {
        var dueOn = command.DueOn ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var bills = await db.RecurringBills
            .Include(x => x.Occurrences)
            .Where(x => x.NextDueOn <= dueOn)
            .OrderBy(x => x.NextDueOn)
            .ToListAsync(ct);

        foreach (var bill in bills)
        {
            while (bill.IsDue(dueOn))
            {
                var account = await db.Accounts.SingleAsync(x => x.HouseholdId == bill.HouseholdId && x.Id == bill.AccountId, ct);
                Category? category = null;
                if (bill.CategoryId is not null)
                {
                    category = await db.Categories.SingleAsync(x => x.HouseholdId == bill.HouseholdId && x.Id == bill.CategoryId, ct);
                }

                if (bill.Type == RecurringBillType.Fixed)
                {
                    var posted = bill.PostDue(account, category, bill.NextDueOn);
                    if (posted.IsError)
                    {
                        break;
                    }

                    db.Transactions.Add(posted.Value);
                    db.Entry(bill.Occurrences.Single(x => x.TransactionId == posted.Value.Id)).State = EntityState.Added;
                }
                else
                {
                    var pending = bill.CreatePending(account, category, bill.NextDueOn);
                    if (pending.IsError)
                    {
                        break;
                    }

                    db.Transactions.Add(pending.Value);
                    db.Entry(bill.Occurrences.Single(x => x.TransactionId == pending.Value.Id)).State = EntityState.Added;
                    await bus.PublishAsync(new EstimatedBillPendingV1(
                        bill.Id.Value,
                        pending.Value.Id.Value,
                        bill.HouseholdId,
                        bill.AccountId.Value,
                        bill.CategoryId?.Value,
                        pending.Value.Amount.Amount,
                        pending.Value.Amount.Currency,
                        pending.Value.OccurredOn,
                        Guid.NewGuid()));
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
