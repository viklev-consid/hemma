using System.Diagnostics;
using System.Diagnostics.Metrics;
using ErrorOr;

namespace Hemma.Modules.Economy;

internal static class EconomyTelemetry
{
    internal const string SourceName = "Hemma.Modules.Economy";
    internal const string MeterName = "Hemma.Modules.Economy";

    internal static readonly ActivitySource ActivitySource = new(SourceName, "1.0.0");

    private static readonly Meter meter = new(MeterName, "1.0.0");

    internal static readonly Counter<long> CommandsHandled =
        meter.CreateCounter<long>(
            "hemma.economy.commands.handled",
            description: "Total commands successfully handled by the Economy module.");

    internal static readonly Counter<long> CommandsFailed =
        meter.CreateCounter<long>(
            "hemma.economy.commands.failed",
            description: "Total command failures in the Economy module.");

    internal static readonly Counter<long> EventsPublished =
        meter.CreateCounter<long>(
            "hemma.economy.events.published",
            description: "Total integration events published by the Economy module.");

    internal static readonly Counter<long> EventsProcessed =
        meter.CreateCounter<long>(
            "hemma.economy.events.processed",
            description: "Total integration events processed by the Economy module.");

    internal static async Task<ErrorOr<T>> InstrumentAsync<T>(
        string operation,
        Func<Task<ErrorOr<T>>> handler)
    {
        using var activity = ActivitySource.StartActivity(operation);
        try
        {
            var result = await handler();
            if (result.IsError)
            {
                activity?.SetStatus(ActivityStatusCode.Error, result.FirstError.Description);
                CommandsFailed.Add(1, new KeyValuePair<string, object?>("operation", operation));
            }
            else
            {
                CommandsHandled.Add(1, new KeyValuePair<string, object?>("operation", operation));
            }

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            CommandsFailed.Add(1, new KeyValuePair<string, object?>("operation", operation));
            throw;
        }
    }
}
