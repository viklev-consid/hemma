using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.CreateRecurringBill;

public sealed class CreateRecurringBillHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<RecurringBillResponse>> Handle(CreateRecurringBillCommand cmd, CancellationToken ct)
    {
        var accountId = new AccountId(cmd.AccountId);
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == accountId, ct);
        if (account is null)
        {
            return EconomyErrors.AccountNotFound;
        }

        Category? category = null;
        if (cmd.CategoryId is not null)
        {
            var categoryId = new CategoryId(cmd.CategoryId.Value);
            category = await db.Categories.SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == categoryId, ct);
            if (category is null)
            {
                return EconomyErrors.CategoryNotFound;
            }
        }

        var amount = Money.Create(cmd.Amount, cmd.Currency);
        if (amount.IsError)
        {
            return amount.Errors;
        }

        var cadence = RecurringBillCadence.Create(cmd.CadenceFrequency, cmd.CadenceInterval, cmd.CadenceDayOfMonth);
        if (cadence.IsError)
        {
            return cadence.Errors;
        }

        var type = RecurringBillType.Create(cmd.Type);
        if (type.IsError)
        {
            return type.Errors;
        }

        var direction = RecurringBillDirection.Create(cmd.Direction);
        if (direction.IsError)
        {
            return direction.Errors;
        }

        var bill = RecurringBill.Create(
            cmd.HouseholdId,
            cmd.Name,
            account,
            category,
            amount.Value,
            cadence.Value,
            type.Value,
            direction.Value,
            cmd.StartsOn,
            cmd.Note);
        if (bill.IsError)
        {
            return bill.Errors;
        }

        db.RecurringBills.Add(bill.Value);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(bill.Value.HouseholdId, "economy.recurring_bill.created", "RecurringBill", bill.Value.Id.Value, null, ct);

        return RecurringBillResponse.From(bill.Value);
    }
}
