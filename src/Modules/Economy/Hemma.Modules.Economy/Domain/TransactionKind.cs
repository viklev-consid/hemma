using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record TransactionKind
{
    public static readonly TransactionKind Expense = new("Expense");
    public static readonly TransactionKind Income = new("Income");
    public static readonly TransactionKind Transfer = new("Transfer");

    private static readonly Dictionary<string, TransactionKind> known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Expense.Name] = Expense,
            [Income.Name] = Income,
            [Transfer.Name] = Transfer
        };

    private TransactionKind(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public static ErrorOr<TransactionKind> Create(string name) =>
        known.TryGetValue(name.Trim(), out var kind)
            ? kind
            : EconomyErrors.TransactionKindInvalid;

    public override string ToString() => Name;
}
