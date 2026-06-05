using Hemma.Modules.Economy.Contracts.Events;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hemma.Modules.Economy.Jobs;

public sealed class TrialRenewalReminderHandler(EconomyDbContext db, IClock clock, IMessageBus bus)
{
    public async Task Handle(TrialRenewalReminder command, CancellationToken ct)
    {
        var today = command.Today ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var through = today.AddDays(command.DaysAhead);

        var trials = await db.Subscriptions
            .AsNoTracking()
            .Where(subscription =>
                subscription.LifecycleState == SubscriptionLifecycleState.Trial &&
                subscription.TrialEndsOn >= today &&
                subscription.TrialEndsOn <= through)
            .Select(subscription => new
            {
                subscription.Id,
                subscription.HouseholdId,
                subscription.Name,
                subscription.TrialEndsOn
            })
            .ToListAsync(ct);

        foreach (var trial in trials)
        {
            await bus.PublishAsync(
                new TrialRenewalDueV1(
                    trial.Id.Value,
                    trial.HouseholdId,
                    trial.Name,
                    trial.TrialEndsOn!.Value,
                    Guid.NewGuid()));
        }
    }
}
