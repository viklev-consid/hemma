using System.Text.Json;
using Hemma.Modules.Audit.Domain;
using Hemma.Modules.Audit.Persistence;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

/// <summary>Audit handler for confirmed email changes (Phase 9.5 email-change flow).</summary>
[NonTransactional]
public sealed class OnEmailChangedHandler(AuditDbContext db, IClock clock)
{
    public async Task Handle(EmailChangedV1 @event, CancellationToken ct)
    {
        using var activity = AuditTelemetry.ActivitySource.StartActivity(nameof(OnEmailChangedHandler));
        AuditTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(EmailChangedV1)));

        var payload = JsonSerializer.Serialize(new { @event.UserId });
        var entry = AuditEntry.Create(
            eventType: "user.email_changed",
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
            // Idempotency: duplicate delivery — audit entry already recorded, nothing to do.
        }
    }
}
