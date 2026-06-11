using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Economy.Domain;

public sealed record BudgetId(Guid Value) : TypedId<Guid>(Value)
{
    public static BudgetId New() => new(Guid.NewGuid());
}
