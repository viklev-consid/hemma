using ErrorOr;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.Domain;

public sealed class Subscription : AggregateRoot<SubscriptionId>
{
    private Subscription(
        SubscriptionId id,
        Guid householdId,
        string name,
        SubscriptionCadence cadence,
        Money expectedAmount,
        SubscriptionLifecycleState lifecycleState,
        DateOnly? trialEndsOn,
        AccountId? accountId,
        DateOnly startsOn) : base(id)
    {
        HouseholdId = householdId;
        Name = name;
        Cadence = cadence;
        ExpectedAmount = expectedAmount;
        LifecycleState = lifecycleState;
        TrialEndsOn = trialEndsOn;
        AccountId = accountId;
        StartsOn = startsOn;
    }

    private Subscription() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SubscriptionCadence Cadence { get; private set; } = null!;
    public Money ExpectedAmount { get; private set; } = null!;
    public SubscriptionLifecycleState LifecycleState { get; private set; } = null!;
    public DateOnly? TrialEndsOn { get; private set; }
    public DateOnly? TrialReminderSentForTrialEndsOn { get; private set; }
    public AccountId? AccountId { get; private set; }
    public DateOnly StartsOn { get; private set; }
    public DateOnly? CancelledOn { get; private set; }

    public static ErrorOr<Subscription> Create(
        Guid householdId,
        string name,
        SubscriptionCadence cadence,
        Money expectedAmount,
        SubscriptionLifecycleState lifecycleState,
        DateOnly? trialEndsOn,
        Account? account,
        DateOnly startsOn)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
        {
            return EconomyErrors.SubscriptionInvalid;
        }

        if (account is not null && account.HouseholdId != householdId)
        {
            return EconomyErrors.AccountNotFound;
        }

        if (lifecycleState == SubscriptionLifecycleState.Trial && trialEndsOn is null)
        {
            return EconomyErrors.SubscriptionTrialEndRequired;
        }

        if (lifecycleState == SubscriptionLifecycleState.Cancelled)
        {
            return EconomyErrors.SubscriptionLifecycleStateInvalid;
        }

        return new Subscription(
            SubscriptionId.New(),
            householdId,
            name.Trim(),
            cadence,
            expectedAmount,
            lifecycleState,
            lifecycleState == SubscriptionLifecycleState.Trial ? trialEndsOn : null,
            account?.Id,
            startsOn);
    }

    public ErrorOr<Success> ChangeLifecycleState(SubscriptionLifecycleState state, DateOnly? trialEndsOn, DateOnly today)
    {
        if (LifecycleState == SubscriptionLifecycleState.Cancelled)
        {
            return EconomyErrors.SubscriptionLifecycleStateInvalid;
        }

        if (state == SubscriptionLifecycleState.Trial && trialEndsOn is null)
        {
            return EconomyErrors.SubscriptionTrialEndRequired;
        }

        LifecycleState = state;
        TrialEndsOn = state == SubscriptionLifecycleState.Trial ? trialEndsOn : null;
        TrialReminderSentForTrialEndsOn = state == SubscriptionLifecycleState.Trial ? TrialReminderSentForTrialEndsOn : null;
        CancelledOn = state == SubscriptionLifecycleState.Cancelled ? today : null;
        return Result.Success;
    }

    public bool ShouldSendTrialReminder() =>
        LifecycleState == SubscriptionLifecycleState.Trial &&
        TrialEndsOn is not null &&
        TrialReminderSentForTrialEndsOn != TrialEndsOn;

    public void MarkTrialReminderSent() => TrialReminderSentForTrialEndsOn = TrialEndsOn;
}
