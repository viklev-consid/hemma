using TickerQ.Utilities.Base;
using Wolverine;

namespace Hemma.Modules.Property.Jobs;

public sealed class MaterializeMaintenanceOccurrencesJob(IMessageBus bus)
{
    public const string Name = "property.materialize-maintenance";
    public const string CronExpression = "0 0 6 * * *";

    [TickerFunction(Name, CronExpression)]
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken ct)
    {
        await bus.InvokeAsync(new MaterializeMaintenanceOccurrences(), ct);
    }
}
