using ErrorOr;
using Hemma.Modules.Households.Contracts.Queries;
using Hemma.Modules.Notifications.Contracts.Commands;
using Hemma.Modules.Notifications.Contracts.Dtos;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Hemma.Modules.Property.Jobs;

public sealed partial class PropertyNotificationDispatcher(
    IMessageBus bus,
    IClock clock,
    ILogger<PropertyNotificationDispatcher> logger)
{
    private readonly Dictionary<Guid, IReadOnlyList<HouseholdMemberInfo>> membersByHousehold = [];

    public async Task NotifyHouseholdAsync(PropertyNotification notification, CancellationToken ct)
    {
        if (!membersByHousehold.TryGetValue(notification.HouseholdId, out var members))
        {
            var result = await bus.InvokeAsync<ListHouseholdMembersResult>(
                new ListHouseholdMembersQuery(notification.HouseholdId), ct);
            members = result.Members;
            membersByHousehold[notification.HouseholdId] = members;
        }

        foreach (var member in members)
        {
            await NotifyMemberAsync(notification, member.UserId, ct);
        }
    }

    private async Task NotifyMemberAsync(PropertyNotification notification, Guid userId, CancellationToken ct)
    {
        var idempotencyKey = DeterministicGuid.Create(
            notification.Source,
            notification.SourceId.ToString("D"),
            userId.ToString("D"),
            notification.Kind,
            notification.RelevantDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

        var command = new CreateNotificationCommand(
            userId,
            notification.Type,
            NotificationCategory.Product,
            notification.Severity,
            notification.Title,
            notification.Body,
            notification.Link,
            Channels: null,
            idempotencyKey,
            clock.UtcNow);

        try
        {
            await bus.InvokeAsync<ErrorOr<CreateNotificationResponse>>(command, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogNotificationFailed(logger, notification.Source, notification.SourceId, notification.Kind, userId, ex);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to send property notification {Kind} for {Source} {SourceId} to user {UserId}.")]
    private static partial void LogNotificationFailed(ILogger logger, string source, Guid sourceId, string kind, Guid userId, Exception exception);
}
