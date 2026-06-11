using TickerQ.Utilities.Base;
using Wolverine;

namespace Hemma.Modules.Economy.Jobs;

public sealed class TrialRenewalReminderJob(IMessageBus bus)
{
    public const string Name = "economy.trial-renewal-reminders";
    public const string CronExpression = "0 0 9 * * *";

    [TickerFunction(Name, CronExpression)]
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken ct)
    {
        await bus.InvokeAsync(new TrialRenewalReminder(), ct);
    }
}
