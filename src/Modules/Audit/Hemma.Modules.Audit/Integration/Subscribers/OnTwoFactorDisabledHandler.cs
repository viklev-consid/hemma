using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Audit.Domain;
using Hemma.Modules.Audit.Persistence;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

[NonTransactional]
public sealed class OnTwoFactorDisabledHandler(AuditDbContext db, IClock clock)
{
    public async Task Handle(TwoFactorDisabledV1 @event, CancellationToken ct)
    {
        using var activity = AuditTelemetry.ActivitySource.StartActivity(nameof(OnTwoFactorDisabledHandler));
        AuditTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(TwoFactorDisabledV1)));

        var payload = JsonSerializer.Serialize(new { @event.UserId, @event.Method });
        var entry = AuditEntry.Create(
            eventType: "user.two_factor_disabled",
            actorId: @event.UserId,
            resourceType: "User",
            resourceId: @event.UserId,
            payload: payload,
            occurredAt: clock.UtcNow,
            idempotencyKey: @event.EventId);

        db.AuditEntries.Add(entry);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            db.Entry(entry).State = EntityState.Detached;
        }
    }
}
