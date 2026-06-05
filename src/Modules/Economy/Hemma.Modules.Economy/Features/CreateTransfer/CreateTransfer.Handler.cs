using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.CreateTransfer;

public sealed class CreateTransferHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<CreateTransferResponse>> Handle(CreateTransferCommand cmd, CancellationToken ct)
    {
        var fromAccountId = new AccountId(cmd.FromAccountId);
        var toAccountId = new AccountId(cmd.ToAccountId);
        var accounts = await db.Accounts
            .Where(account => account.HouseholdId == cmd.HouseholdId && (account.Id == fromAccountId || account.Id == toAccountId))
            .ToListAsync(ct);
        var from = accounts.SingleOrDefault(account => account.Id == fromAccountId);
        var to = accounts.SingleOrDefault(account => account.Id == toAccountId);
        if (from is null || to is null)
        {
            return EconomyErrors.AccountNotFound;
        }

        var mode = TransferMode.Create(cmd.Mode);
        if (mode.IsError)
        {
            return mode.Errors;
        }

        var outflowAmount = Money.Create(cmd.Amount, cmd.Currency);
        if (outflowAmount.IsError)
        {
            return outflowAmount.Errors;
        }

        var inflowAmount = Money.Create(cmd.Amount, cmd.Currency);
        if (inflowAmount.IsError)
        {
            return inflowAmount.Errors;
        }

        Category? category = null;
        if (mode.Value == TransferMode.Savings)
        {
            category = await ResolveSavingsCategoryAsync(cmd.HouseholdId, cmd.CategoryId, ct);
            if (category is null)
            {
                return EconomyErrors.CategoryNotFound;
            }
        }

        var transferId = TransferId.New();
        var outflow = Transaction.CreateTransferLeg(cmd.HouseholdId, from, category, outflowAmount.Value, cmd.OccurredOn, cmd.Note, cmd.PayerId, transferId, isOutflow: true);
        if (outflow.IsError)
        {
            return outflow.Errors;
        }

        var inflow = Transaction.CreateTransferLeg(cmd.HouseholdId, to, null, inflowAmount.Value, cmd.OccurredOn, cmd.Note, cmd.PayerId, transferId, isOutflow: false);
        if (inflow.IsError)
        {
            return inflow.Errors;
        }

        var transfer = Transfer.Create(cmd.HouseholdId, outflow.Value, inflow.Value, mode.Value);
        if (transfer.IsError)
        {
            return transfer.Errors;
        }

        db.Transactions.AddRange(outflow.Value, inflow.Value);
        await db.SaveChangesAsync(ct);

        db.Transfers.Add(transfer.Value);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(transfer.Value.HouseholdId, "economy.transfer.created", "Transfer", transfer.Value.Id.Value, null, ct);

        return new CreateTransferResponse(
            transfer.Value.Id.Value,
            transfer.Value.Mode.Name,
            TransactionResponse.From(outflow.Value),
            TransactionResponse.From(inflow.Value));
    }

    private async Task<Category?> ResolveSavingsCategoryAsync(Guid householdId, Guid? categoryId, CancellationToken ct)
    {
        if (categoryId is not null)
        {
            var id = new CategoryId(categoryId.Value);
            return await db.Categories.SingleOrDefaultAsync(category => category.HouseholdId == householdId && category.Id == id, ct);
        }

        return await db.Categories
            .Where(category => category.HouseholdId == householdId && category.Budgetable && category.ParentCategoryId == null)
            .OrderByDescending(category => category.Name == "Savings")
            .ThenBy(category => category.Name)
            .FirstOrDefaultAsync(ct);
    }
}
