using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Households.Domain;

public sealed record HouseholdId(Guid Value) : TypedId<Guid>(Value)
{
    public static HouseholdId New() => new(Guid.NewGuid());
}
