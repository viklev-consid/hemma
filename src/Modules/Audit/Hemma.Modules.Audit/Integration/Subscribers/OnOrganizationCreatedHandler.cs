using System.Text.Json;
using Hemma.Modules.Organizations.Contracts.Events;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

[NonTransactional]
public sealed class OnOrganizationCreatedHandler(OrganizationAuditWriter writer)
{
    public async Task Handle(OrganizationCreatedV1 @event, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new { @event.OrganizationId });
        await writer.WriteAsync(
            "organization.created",
            @event.CreatedByUserId,
            @event.OrganizationId,
            "Organization",
            @event.OrganizationId,
            payload,
            @event.EventId,
            ct);
    }
}
