using System.Text.Json;
using Hemma.Modules.Audit.Domain;
using Hemma.Modules.Audit.Persistence;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

[NonTransactional]
public sealed class OnUserRoleChangedHandler(AuditDbContext db, IClock clock)
{
    public async Task Handle(UserRoleChangedV1 @event, CancellationToken ct)
    {
        using var activity = AuditTelemetry.ActivitySource.StartActivity(nameof(OnUserRoleChangedHandler));
        AuditTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(UserRoleChangedV1)));

        var payload = JsonSerializer.Serialize(new
        {
            @event.UserId,
            @event.OldRole,
            @event.NewRole,
            @event.ChangedBy,
        });

        var entry = AuditEntry.Create(
            eventType: "user.role_changed",
            actorId: @event.ChangedBy,
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
