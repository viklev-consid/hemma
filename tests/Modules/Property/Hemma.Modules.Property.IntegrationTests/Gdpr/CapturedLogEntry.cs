using Microsoft.Extensions.Logging;

namespace Hemma.Modules.Property.IntegrationTests.Gdpr;

public sealed record CapturedLogEntry(string Category, LogLevel Level, string Message, Exception? Exception);
