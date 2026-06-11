using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Hemma.Modules.Property.IntegrationTests.Gdpr;

/// <summary>
/// In-memory <see cref="ILogger{T}"/> that records every emitted entry. Constructed directly
/// into the unit under test so assertions don't depend on the host's Serilog pipeline.
/// </summary>
public sealed class CapturingLogger<T> : ILogger<T>
{
    public ConcurrentQueue<CapturedLogEntry> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Enqueue(new CapturedLogEntry(typeof(T).FullName ?? typeof(T).Name, logLevel, formatter(state, exception), exception));
    }
}
