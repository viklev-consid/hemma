using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Catalog.Persistence;
using Hemma.Modules.Users.Contracts.Events;
using Wolverine.Attributes;

namespace Hemma.Modules.Catalog.Integration.Subscribers;

[NonTransactional]
public sealed class OnEmailChangedHandler(CatalogDbContext db)
{
    public async Task Handle(EmailChangedV1 @event, CancellationToken ct)
    {
        using var activity = CatalogTelemetry.ActivitySource.StartActivity(nameof(OnEmailChangedHandler));
        CatalogTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(EmailChangedV1)));

        await db.Customers
            .Where(c => c.UserId == @event.UserId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Email, @event.NewEmail), ct);
    }
}
