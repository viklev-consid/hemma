using Hemma.Modules.Notifications.Domain;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Notifications.Templates;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Shared.Infrastructure.Notifications;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace Hemma.Modules.Notifications.Integration.Subscribers;

/// <summary>
/// Alerts the OLD email address after a confirmed email change — defence-in-depth
/// against silent account takeover.
/// </summary>
[NonTransactional]
public sealed class OnEmailChangedHandler(
    NotificationsDbContext db,
    IEmailSender emailSender,
    IClock clock,
    NotificationSendGuard sendGuard,
    IEmailTemplateRenderer templateRenderer)
{
    public async Task Handle(EmailChangedV1 @event, CancellationToken ct)
    {
        using var activity = NotificationsTelemetry.ActivitySource.StartActivity(nameof(OnEmailChangedHandler));
        NotificationsTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(EmailChangedV1)));

        // Send to the OLD email — that is the address that needs the alert.
        var log = NotificationLog.Create(
            @event.UserId, @event.OldEmail, NotificationType.EmailChanged,
            EmailChangedTemplate.Subject, clock.UtcNow, @event.EventId);
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

        var renderedTemplate = templateRenderer.Render(
            EmailTemplateId.EmailChanged,
            new EmailChangedModel(@event.NewEmail));
        if (renderedTemplate.IsError)
        {
            await sendGuard.MarkFailedAsync(@event.EventId, leaseToken, ct);
            return;
        }

        var message = new EmailMessage(
            To: @event.OldEmail,
            Subject: renderedTemplate.Value.Subject,
            HtmlBody: renderedTemplate.Value.HtmlBody,
            PlainTextBody: EmailChangedTemplate.PlainTextBody(@event.NewEmail));

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
