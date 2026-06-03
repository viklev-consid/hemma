using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Notifications.Domain;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Notifications.Templates;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Shared.Infrastructure.Notifications;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine.Attributes;

namespace Hemma.Modules.Notifications.Integration.Subscribers;

[NonTransactional]
public sealed class OnRecoveryCodesRegeneratedHandler(
    NotificationsDbContext db,
    IEmailSender emailSender,
    IClock clock,
    NotificationSendGuard sendGuard,
    IEmailTemplateRenderer templateRenderer)
{
    public async Task Handle(RecoveryCodesRegeneratedV1 @event, CancellationToken ct)
    {
        using var activity = NotificationsTelemetry.ActivitySource.StartActivity(nameof(OnRecoveryCodesRegeneratedHandler));
        NotificationsTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(RecoveryCodesRegeneratedV1)));

        var log = NotificationLog.Create(@event.UserId, @event.Email, NotificationType.RecoveryCodesRegenerated, RecoveryCodesRegeneratedTemplate.Subject, clock.UtcNow, @event.EventId);
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
            EmailTemplateId.RecoveryCodesRegenerated,
            EmptyEmailTemplateModel.Instance);
        if (renderedTemplate.IsError)
        {
            await sendGuard.MarkFailedAsync(@event.EventId, leaseToken, ct);
            return;
        }

        var message = new EmailMessage(@event.Email, renderedTemplate.Value.Subject, renderedTemplate.Value.HtmlBody, RecoveryCodesRegeneratedTemplate.PlainTextBody);
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
