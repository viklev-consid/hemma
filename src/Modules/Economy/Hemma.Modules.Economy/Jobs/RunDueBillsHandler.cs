using Hemma.Modules.Economy.Contracts.Events;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Hemma.Modules.Economy.Jobs;

public sealed partial class RunDueBillsHandler(EconomyDbContext db, IClock clock, IMessageBus bus, ILogger<RunDueBillsHandler> logger)
{
    private const int BatchSize = 100;

    public async Task Handle(RunDueBills command, CancellationToken ct)
    {
        var dueOn = command.DueOn ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var processed = new HashSet<RecurringBillId>();

        while (true)
        {
            var bills = await db.RecurringBills
                .Include(x => x.Occurrences)
                .Where(x => x.NextDueOn <= dueOn && !processed.Contains(x.Id))
                .OrderBy(x => x.NextDueOn)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (bills.Count == 0)
            {
                break;
            }

            foreach (var bill in bills)
            {
                processed.Add(bill.Id);
                try
                {
                    await ProcessBillAsync(bill, dueOn, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogRecurringBillFailed(logger, bill.Id.Value, dueOn, ex);
                    db.ChangeTracker.Clear();
                }
            }
        }
    }

    private async Task ProcessBillAsync(RecurringBill bill, DateOnly dueOn, CancellationToken ct)
    {
        var pendingEvents = new List<EstimatedBillPendingV1>();

        while (bill.IsDue(dueOn))
        {
            var account = await db.Accounts.SingleOrDefaultAsync(x => x.HouseholdId == bill.HouseholdId && x.Id == bill.AccountId, ct);
            if (account is null)
            {
                LogMissingAccount(logger, bill.Id.Value, bill.AccountId.Value);
                break;
            }

            Category? category = null;
            if (bill.CategoryId is not null)
            {
                category = await db.Categories.SingleOrDefaultAsync(x => x.HouseholdId == bill.HouseholdId && x.Id == bill.CategoryId, ct);
                if (category is null)
                {
                    LogMissingCategory(logger, bill.Id.Value, bill.CategoryId.Value);
                    break;
                }
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
                pendingEvents.Add(new EstimatedBillPendingV1(
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

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            LogDuplicateOccurrence(logger, bill.Id.Value, ex);
            db.ChangeTracker.Clear();
            return;
        }

        foreach (var pendingEvent in pendingEvents)
        {
            await bus.PublishAsync(pendingEvent);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to process recurring bill {RecurringBillId} due by {DueOn}.")]
    private static partial void LogRecurringBillFailed(ILogger logger, Guid recurringBillId, DateOnly dueOn, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Recurring bill {RecurringBillId} references missing account {AccountId}.")]
    private static partial void LogMissingAccount(ILogger logger, Guid recurringBillId, Guid accountId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Recurring bill {RecurringBillId} references missing category {CategoryId}.")]
    private static partial void LogMissingCategory(ILogger logger, Guid recurringBillId, Guid categoryId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Recurring bill {RecurringBillId} already has one or more due occurrences.")]
    private static partial void LogDuplicateOccurrence(ILogger logger, Guid recurringBillId, Exception exception);
}
