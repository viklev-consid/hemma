using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record RecurringBillOccurrenceState
{
    public static readonly RecurringBillOccurrenceState Pending = new("Pending");
    public static readonly RecurringBillOccurrenceState Posted = new("Posted");
    public static readonly RecurringBillOccurrenceState Confirmed = new("Confirmed");
    public static readonly RecurringBillOccurrenceState Skipped = new("Skipped");
    public static readonly RecurringBillOccurrenceState Paused = new("Paused");

    private static readonly Dictionary<string, RecurringBillOccurrenceState> known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Pending.Name] = Pending,
            [Posted.Name] = Posted,
            [Confirmed.Name] = Confirmed,
            [Skipped.Name] = Skipped,
            [Paused.Name] = Paused
        };

    private RecurringBillOccurrenceState(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public static ErrorOr<RecurringBillOccurrenceState> Create(string name) =>
        !string.IsNullOrWhiteSpace(name) && known.TryGetValue(name.Trim(), out var state)
            ? state
            : EconomyErrors.RecurringBillOccurrenceInvalid;

    public override string ToString() => Name;
}
