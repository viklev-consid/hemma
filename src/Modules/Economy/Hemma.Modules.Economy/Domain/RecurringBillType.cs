using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record RecurringBillType
{
    public static readonly RecurringBillType Fixed = new("Fixed");
    public static readonly RecurringBillType Estimated = new("Estimated");

    private static readonly Dictionary<string, RecurringBillType> known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Fixed.Name] = Fixed,
            [Estimated.Name] = Estimated
        };

    private RecurringBillType(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public static ErrorOr<RecurringBillType> Create(string name) =>
        !string.IsNullOrWhiteSpace(name) && known.TryGetValue(name.Trim(), out var type)
            ? type
            : EconomyErrors.RecurringBillTypeInvalid;

    public override string ToString() => Name;
}
