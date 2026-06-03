using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Notifications.Domain;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Notifications.Templates;
using Hemma.Modules.Organizations.Contracts.Events;
using Hemma.Shared.Infrastructure.Frontend;
using Hemma.Shared.Infrastructure.Notifications;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine.Attributes;

namespace Hemma.Modules.Notifications.Integration.Subscribers;

[NonTransactional]
public sealed class OnOrganizationInvitationCreatedHandler(
    NotificationsDbContext db,
    IEmailSender emailSender,
    IClock clock,
    NotificationSendGuard sendGuard,
    IFrontendUrlBuilder frontendUrls,
    IEmailTemplateRenderer templateRenderer)
{
    public async Task Handle(OrganizationInvitationCreatedV1 @event, CancellationToken ct)
    {
        using var activity = NotificationsTelemetry.ActivitySource.StartActivity(nameof(OnOrganizationInvitationCreatedHandler));
        NotificationsTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(OrganizationInvitationCreatedV1)));

        var log = NotificationLog.Create(
            Guid.Empty,
            @event.Email,
            NotificationType.OrganizationInvitation,
            OrganizationInvitationTemplate.Subject,
            clock.UtcNow,
            @event.EventId);
        db.NotificationLogs.Add(log);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            db.Entry(log).State = EntityState.Detached;
        }

        if (await sendGuard.TryClaimAsync(@event.EventId, ct) is not { } leaseToken)
        {
            return;
        }

        var invitationUrl = frontendUrls.AcceptOrganizationInvitation(@event.RawToken, @event.Email);
        var renderedTemplate = templateRenderer.Render(
            EmailTemplateId.OrganizationInvitation,
            new OrganizationInvitationModel(@event.Role, @event.RawToken, invitationUrl));
        if (renderedTemplate.IsError)
        {
            await sendGuard.MarkFailedAsync(@event.EventId, leaseToken, ct);
            return;
        }

        var message = new EmailMessage(
            To: @event.Email,
            Subject: renderedTemplate.Value.Subject,
            HtmlBody: renderedTemplate.Value.HtmlBody,
            PlainTextBody: OrganizationInvitationTemplate.PlainTextBody(@event.Role, @event.RawToken, invitationUrl));

        try
        {
            await sendGuard.SendWithLeaseRenewalAsync(
                @event.EventId, leaseToken,
                token => emailSender.SendAsync(message, token), ct);
        }
        catch (RetryableSmtpException)
        {
            await sendGuard.MarkReadyAsync(@event.EventId, leaseToken, ct);
            throw;
        }
        catch (TerminalSmtpException)
        {
            await sendGuard.MarkFailedAsync(@event.EventId, leaseToken, ct);
            throw;
        }

        await sendGuard.MarkSentAsync(@event.EventId, leaseToken, ct);
    }
}
