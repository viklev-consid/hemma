using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Households.Domain;

public sealed record HouseholdInvitationId(Guid Value) : TypedId<Guid>(Value)
{
    public static HouseholdInvitationId New() => new(Guid.NewGuid());
}
