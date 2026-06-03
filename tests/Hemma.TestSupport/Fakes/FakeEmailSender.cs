using Hemma.Shared.Infrastructure.Notifications;

namespace Hemma.TestSupport.Fakes;

public sealed class FakeEmailSender : IEmailSender
{
    private readonly List<EmailMessage> sent = [];

    public IReadOnlyList<EmailMessage> SentMessages => sent.AsReadOnly();

    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        sent.Add(message);
        return Task.CompletedTask;
    }
}
