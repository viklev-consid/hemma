using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Organizations.Domain;

public sealed record OrganizationId(Guid Value) : TypedId<Guid>(Value)
{
    public static OrganizationId New() => new(Guid.NewGuid());
}
