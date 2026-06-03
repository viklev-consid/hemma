using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Shared.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
