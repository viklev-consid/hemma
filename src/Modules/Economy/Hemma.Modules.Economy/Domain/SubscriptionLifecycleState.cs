using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record SubscriptionLifecycleState
{
    public static readonly SubscriptionLifecycleState Trial = new("Trial");
    public static readonly SubscriptionLifecycleState Active = new("Active");
    public static readonly SubscriptionLifecycleState Paused = new("Paused");
    public static readonly SubscriptionLifecycleState Cancelled = new("Cancelled");

    private static readonly Dictionary<string, SubscriptionLifecycleState> known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Trial.Name] = Trial,
            [Active.Name] = Active,
            [Paused.Name] = Paused,
            [Cancelled.Name] = Cancelled
        };

    private SubscriptionLifecycleState(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public static ErrorOr<SubscriptionLifecycleState> Create(string name) =>
        !string.IsNullOrWhiteSpace(name) && known.TryGetValue(name.Trim(), out var state)
            ? state
            : EconomyErrors.SubscriptionLifecycleStateInvalid;

    public override string ToString() => Name;
}
