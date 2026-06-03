using System.Threading.Channels;
using Hemma.Modules.Notifications.Streaming;

namespace Hemma.Modules.Notifications.Features.StreamMyNotifications;

public sealed record StreamMyNotificationsResponse(
    ChannelReader<NotificationStreamEvent> Reader,
    IDisposable Subscription);
