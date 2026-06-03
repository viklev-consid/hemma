using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Audit.Domain;
using Hemma.Modules.Audit.Persistence;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Audit.Integration.Subscribers;

public sealed class OrganizationAuditWriter(AuditDbContext db, IClock clock)
{
    public async Task WriteAsync(
        string eventType,
        Guid? actorId,
        Guid organizationId,
        string resourceType,
        Guid resourceId,
        string payload,
        Guid eventId,
        CancellationToken ct)
    {
        var entry = AuditEntry.Create(
            eventType,
            actorId,
            resourceType,
            resourceId,
            payload,
            clock.UtcNow,
            eventId,
            organizationId,
            accessMode: "event");

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
