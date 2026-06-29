using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Property.Domain;

public sealed record ProjectId(Guid Value) : TypedId<Guid>(Value)
{
    public static ProjectId New() => new(Guid.NewGuid());
}
