using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Organizations.Domain;

public sealed record OrganizationInvitationId(Guid Value) : TypedId<Guid>(Value)
{
    public static OrganizationInvitationId New() => new(Guid.NewGuid());
}
