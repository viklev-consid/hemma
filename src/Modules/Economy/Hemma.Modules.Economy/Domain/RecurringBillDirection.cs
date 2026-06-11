using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record RecurringBillDirection
{
    public static readonly RecurringBillDirection Expense = new("Expense");
    public static readonly RecurringBillDirection Income = new("Income");

    private static readonly Dictionary<string, RecurringBillDirection> known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Expense.Name] = Expense,
            [Income.Name] = Income
        };

    private RecurringBillDirection(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public TransactionKind ToTransactionKind() =>
        this == Income ? TransactionKind.Income : TransactionKind.Expense;

    public static ErrorOr<RecurringBillDirection> Create(string name) =>
        !string.IsNullOrWhiteSpace(name) && known.TryGetValue(name.Trim(), out var direction)
            ? direction
            : EconomyErrors.RecurringBillDirectionInvalid;

    public override string ToString() => Name;
}
