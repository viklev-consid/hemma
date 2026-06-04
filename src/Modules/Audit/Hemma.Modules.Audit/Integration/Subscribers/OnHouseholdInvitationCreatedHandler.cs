using System.Text.Json;
using Hemma.Modules.Households.Contracts.Events;
using Wolverine.Attributes;

namespace Hemma.Modules.Audit.Integration.Subscribers;

[NonTransactional]
public sealed class OnHouseholdInvitationCreatedHandler(HouseholdAuditWriter writer)
{
    public async Task Handle(HouseholdInvitationCreatedV1 @event, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new { @event.HouseholdId, @event.InvitationId, @event.Role });
        await writer.WriteAsync(
            "household.invitation_created",
            @event.InvitedByUserId,
            @event.HouseholdId,
            "HouseholdInvitation",
            @event.InvitationId,
            payload,
            @event.EventId,
            ct);
    }
}
