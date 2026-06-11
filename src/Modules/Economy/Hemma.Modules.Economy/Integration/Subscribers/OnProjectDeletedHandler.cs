using Hemma.Modules.Economy.Persistence;
using Hemma.Modules.Property.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace Hemma.Modules.Economy.Integration.Subscribers;

// Property owns the project lifecycle; when a project is deleted we clear the link on any
// transactions that pointed at it so the cross-module reference never dangles.
[NonTransactional]
public sealed class OnProjectDeletedHandler(EconomyDbContext db)
{
    public async Task Handle(ProjectDeletedV1 @event, CancellationToken ct)
    {
        using var activity = EconomyTelemetry.ActivitySource.StartActivity(nameof(OnProjectDeletedHandler));
        EconomyTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(ProjectDeletedV1)));

        await db.Transactions
            .Where(transaction => transaction.HouseholdId == @event.HouseholdId && transaction.ProjectId == @event.ProjectId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(transaction => transaction.ProjectId, (Guid?)null), ct);
    }
}
