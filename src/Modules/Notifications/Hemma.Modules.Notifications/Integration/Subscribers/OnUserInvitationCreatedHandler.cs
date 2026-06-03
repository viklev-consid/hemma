using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Notifications.Domain;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Notifications.Templates;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Shared.Infrastructure.Frontend;
using Hemma.Shared.Infrastructure.Notifications;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine.Attributes;

namespace Hemma.Modules.Notifications.Integration.Subscribers;

[NonTransactional]
public sealed class OnUserInvitationCreatedHandler(
    NotificationsDbContext db,
    IEmailSender emailSender,
    IClock clock,
    NotificationSendGuard sendGuard,
    IFrontendUrlBuilder frontendUrls,
    IEmailTemplateRenderer templateRenderer)
{
    public async Task Handle(UserInvitationCreatedV1 @event, CancellationToken ct)
    {
        using var activity = NotificationsTelemetry.ActivitySource.StartActivity(nameof(OnUserInvitationCreatedHandler));
        NotificationsTelemetry.EventsProcessed.Add(1, new KeyValuePair<string, object?>("event", nameof(UserInvitationCreatedV1)));

        var log = NotificationLog.Create(
            Guid.Empty,
            @event.Email,
            NotificationType.UserInvitation,
            UserInvitationTemplate.Subject,
            clock.UtcNow,
            @event.MessageId);
        db.NotificationLogs.Add(log);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            db.Entry(log).State = EntityState.Detached;
        }

        if (await sendGuard.TryClaimAsync(@event.MessageId, ct) is not { } leaseToken)
        {
            return;
        }

        var invitationUrl = frontendUrls.AcceptUserInvitation(@event.Token, @event.Email);
        var renderedTemplate = templateRenderer.Render(
            EmailTemplateId.UserInvitation,
            new UserInvitationModel(@event.Token, invitationUrl));
        if (renderedTemplate.IsError)
        {
            await sendGuard.MarkFailedAsync(@event.MessageId, leaseToken, ct);
            return;
        }

        var message = new EmailMessage(
            To: @event.Email,
            Subject: renderedTemplate.Value.Subject,
            HtmlBody: renderedTemplate.Value.HtmlBody,
            PlainTextBody: UserInvitationTemplate.PlainTextBody(@event.Token, invitationUrl));

        try
        {
            await sendGuard.SendWithLeaseRenewalAsync(
                @event.MessageId, leaseToken,
                token => emailSender.SendAsync(message, token), ct);
        }
        catch (RetryableSmtpException)
        {
            await sendGuard.MarkReadyAsync(@event.MessageId, leaseToken, ct);
            throw;
        }
        catch (TerminalSmtpException)
        {
            await sendGuard.MarkFailedAsync(@event.MessageId, leaseToken, ct);
            throw;
        }

        await sendGuard.MarkSentAsync(@event.MessageId, leaseToken, ct);
    }
}
