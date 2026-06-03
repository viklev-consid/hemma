using System.Diagnostics;
using System.Diagnostics.Metrics;
using ErrorOr;

namespace Hemma.Modules.Organizations;

internal static class OrganizationsTelemetry
{
    internal const string SourceName = "Hemma.Modules.Organizations";
    internal const string MeterName = "Hemma.Modules.Organizations";

    internal static readonly ActivitySource ActivitySource = new(SourceName, "1.0.0");

    private static readonly Meter meter = new(MeterName, "1.0.0");

    internal static readonly Counter<long> CommandsHandled =
        meter.CreateCounter<long>(
            "hemma.organizations.commands.handled",
            description: "Total commands successfully handled by the Organizations module.");

    internal static readonly Counter<long> CommandsFailed =
        meter.CreateCounter<long>(
            "hemma.organizations.commands.failed",
            description: "Total command failures in the Organizations module.");

    internal static readonly Counter<long> EventsPublished =
        meter.CreateCounter<long>(
            "hemma.organizations.events.published",
            description: "Total integration events published by the Organizations module.");

    internal static readonly Counter<long> EventsProcessed =
        meter.CreateCounter<long>(
            "hemma.organizations.events.processed",
            description: "Total integration events processed by the Organizations module.");

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
