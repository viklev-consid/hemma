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
    private readonly Dictionary<Guid, string?> slugByHousehold = [];

    public async Task NotifyHouseholdAsync(PropertyNotification notification, CancellationToken ct)
    {
        if (!membersByHousehold.TryGetValue(notification.HouseholdId, out var members))
        {
            var result = await bus.InvokeAsync<ListHouseholdMembersResult>(
                new ListHouseholdMembersQuery(notification.HouseholdId), ct);
            members = result.Members;
            membersByHousehold[notification.HouseholdId] = members;
        }

        // Property notifications carry a route-relative href (e.g. "/property/issues/{id}"). Every
        // property route lives under the household shell, so the href must arrive already scoped as
        // "/app/h/{slug}/property/...". The slug is owned by Households; resolve it once per household.
        var scoped = notification with { Link = await ScopeLinkAsync(notification, ct) };

        foreach (var member in members)
        {
            await NotifyMemberAsync(scoped, member.UserId, ct);
        }
    }

    private async Task<NotificationLinkDto?> ScopeLinkAsync(PropertyNotification notification, CancellationToken ct)
    {
        if (notification.Link is not { } link)
        {
            return null;
        }

        if (!slugByHousehold.TryGetValue(notification.HouseholdId, out var slug))
        {
            var result = await bus.InvokeAsync<GetHouseholdSlugResult?>(
                new GetHouseholdSlugQuery(notification.HouseholdId), ct);
            slug = result?.Slug;
            slugByHousehold[notification.HouseholdId] = slug;
        }

        if (slug is null)
        {
            // The household could not be resolved (deleted, or a race). Emitting a household-less
            // link would 404 and be dropped by the client sanitiser, so send without a deep link.
            LogSlugUnresolved(logger, notification.HouseholdId, notification.Source, notification.SourceId);
            return null;
        }

        return link with { Href = $"/app/h/{slug}{link.Href}" };
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

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Could not resolve slug for household {HouseholdId}; sending property notification for {Source} {SourceId} without a deep link.")]
    private static partial void LogSlugUnresolved(ILogger logger, Guid householdId, string source, Guid sourceId);
}
