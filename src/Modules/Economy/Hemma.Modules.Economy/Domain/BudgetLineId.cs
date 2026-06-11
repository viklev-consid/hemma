using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Economy.Domain;

public sealed record BudgetLineId(Guid Value) : TypedId<Guid>(Value)
{
    public static BudgetLineId New() => new(Guid.NewGuid());
}
