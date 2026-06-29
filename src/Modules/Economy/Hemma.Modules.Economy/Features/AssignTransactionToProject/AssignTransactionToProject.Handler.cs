using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Hemma.Modules.Property.Contracts.Queries;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hemma.Modules.Economy.Features.AssignTransactionToProject;

public sealed class AssignTransactionToProjectHandler(EconomyDbContext db, EconomyAuditPublisher audit, IMessageBus bus)
{
    public async Task<ErrorOr<TransactionResponse>> Handle(AssignTransactionToProjectCommand cmd, CancellationToken ct)
    {
        var transactionId = new TransactionId(cmd.TransactionId);
        var transaction = await db.Transactions
            .SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == transactionId, ct);
        if (transaction is null)
        {
            return EconomyErrors.TransactionNotFound;
        }

        if (cmd.ProjectId is { } projectId)
        {
            var project = await bus.InvokeAsync<ValidateProjectReferenceResult>(
                new ValidateProjectReferenceQuery(cmd.HouseholdId, projectId),
                ct);

            if (!project.Exists)
            {
                return EconomyErrors.ProjectNotFound;
            }
        }

        transaction.AssignToProject(cmd.ProjectId);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "economy.transaction.project_assigned", "Transaction", transaction.Id.Value, null, ct);

        return TransactionResponse.From(transaction);
    }
}
