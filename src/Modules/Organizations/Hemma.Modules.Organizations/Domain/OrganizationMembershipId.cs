using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Organizations.Domain;

public sealed record OrganizationMembershipId(Guid Value) : TypedId<Guid>(Value)
{
    public static OrganizationMembershipId New() => new(Guid.NewGuid());
}
