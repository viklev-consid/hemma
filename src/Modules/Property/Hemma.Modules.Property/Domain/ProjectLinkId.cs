using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Property.Domain;

public sealed record ProjectLinkId(Guid Value) : TypedId<Guid>(Value)
{
    public static ProjectLinkId New() => new(Guid.NewGuid());
}
