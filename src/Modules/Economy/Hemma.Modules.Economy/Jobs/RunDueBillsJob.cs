using TickerQ.Utilities.Base;
using Wolverine;

namespace Hemma.Modules.Economy.Jobs;

public sealed class RunDueBillsJob(IMessageBus bus)
{
    public const string Name = "economy.run-due-bills";
    public const string CronExpression = "0 0 4 * * *";

    [TickerFunction(Name, CronExpression)]
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken ct)
    {
        await bus.InvokeAsync(new RunDueBills(), ct);
    }
}
