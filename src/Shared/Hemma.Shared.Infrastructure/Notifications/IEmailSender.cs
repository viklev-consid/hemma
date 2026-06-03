namespace Hemma.Shared.Infrastructure.Notifications;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
