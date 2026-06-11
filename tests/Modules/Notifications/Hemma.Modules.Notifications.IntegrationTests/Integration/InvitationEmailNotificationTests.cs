using Hemma.Modules.Households.Contracts.Events;
using Hemma.Modules.Notifications.Domain;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Users.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Tracking;

namespace Hemma.Modules.Notifications.IntegrationTests.Integration;

[Collection("NotificationsCrossModule")]
[Trait("Category", "Integration")]
public sealed class InvitationEmailNotificationTests(NotificationsCrossModuleFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UserInvitationCreated_SendsInvitationEmail()
    {
        var eventId = Guid.NewGuid();
        var invitedByUserId = Guid.NewGuid();
        var invitation = new UserInvitationCreatedV1(
            Guid.NewGuid(),
            "invitee@example.com",
            "user-token",
            DateTimeOffset.UtcNow.AddDays(7),
            invitedByUserId,
            eventId);

        await fixture.ApplicationHost.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(10))
            .PublishMessageAndWaitAsync(invitation);

        var sent = fixture.EmailSender.SentMessages.Single(m =>
            string.Equals(m.To, "invitee@example.com", StringComparison.Ordinal));
        Assert.Contains("https://app.test/register?", sent.PlainTextBody, StringComparison.Ordinal);
        Assert.Contains("token=user-token", sent.PlainTextBody, StringComparison.Ordinal);
        Assert.Contains("email=invitee@example.com", sent.PlainTextBody, StringComparison.Ordinal);
        Assert.Contains("lockEmail=1", sent.PlainTextBody, StringComparison.Ordinal);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var log = await db.NotificationLogs.SingleAsync(l => l.IdempotencyKey == eventId);
        Assert.Equal(Guid.Empty, log.UserId);
        Assert.Equal(NotificationType.UserInvitation, log.NotificationType);
        Assert.Equal(NotificationDeliveryStatus.Sent, log.DeliveryStatus);
    }

    [Fact]
    public async Task HouseholdInvitationCreated_SendsInvitationEmail()
    {
        var eventId = Guid.NewGuid();
        var invitedByUserId = Guid.NewGuid();
        var invitation = new HouseholdInvitationCreatedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "org-invitee@example.com",
            "member",
            "org-token",
            invitedByUserId,
            eventId);

        await fixture.ApplicationHost.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(10))
            .PublishMessageAndWaitAsync(invitation);

        var sent = fixture.EmailSender.SentMessages.Single(m =>
            string.Equals(m.To, "org-invitee@example.com", StringComparison.Ordinal));
        Assert.Contains("https://app.test/invite?", sent.PlainTextBody, StringComparison.Ordinal);
        Assert.Contains("token=org-token", sent.PlainTextBody, StringComparison.Ordinal);
        Assert.Contains("email=org-invitee@example.com", sent.PlainTextBody, StringComparison.Ordinal);
        Assert.Contains("member", sent.PlainTextBody, StringComparison.Ordinal);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var log = await db.NotificationLogs.SingleAsync(l => l.IdempotencyKey == eventId);
        Assert.Equal(Guid.Empty, log.UserId);
        Assert.Equal(NotificationType.HouseholdInvitation, log.NotificationType);
        Assert.Equal(NotificationDeliveryStatus.Sent, log.DeliveryStatus);
    }

    [Fact]
    public async Task HouseholdInvitationCreated_HtmlEncodesUntrustedTemplateValues()
    {
        var invitation = new HouseholdInvitationCreatedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "xss-org-invitee@example.com",
            "<script>alert('role')</script>",
            "<script>alert('token')</script>",
            Guid.NewGuid(),
            Guid.NewGuid());

        await fixture.ApplicationHost.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(10))
            .PublishMessageAndWaitAsync(invitation);

        var sent = fixture.EmailSender.SentMessages.Single(m =>
            string.Equals(m.To, "xss-org-invitee@example.com", StringComparison.Ordinal));
        Assert.DoesNotContain("<script>", sent.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", sent.HtmlBody, StringComparison.Ordinal);
    }
}
