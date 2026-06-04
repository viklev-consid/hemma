using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Households.Domain;

public sealed record HouseholdMembershipId(Guid Value) : TypedId<Guid>(Value)
{
    public static HouseholdMembershipId New() => new(Guid.NewGuid());
}
