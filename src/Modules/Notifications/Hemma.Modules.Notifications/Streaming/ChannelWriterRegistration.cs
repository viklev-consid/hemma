using System.Threading.Channels;

namespace Hemma.Modules.Notifications.Streaming;

public sealed class ChannelWriterRegistration(ChannelWriter<NotificationStreamEvent> writer) : IDisposable
{
    private Action? dispose;

    public ChannelWriter<NotificationStreamEvent> Writer { get; } = writer;

    public void SetDispose(Action onDispose) => dispose = onDispose;

    public void Dispose()
    {
        dispose?.Invoke();
        Writer.TryComplete();
    }
}
