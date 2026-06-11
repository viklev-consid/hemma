using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Property.Domain;

public sealed record ProjectTaskId(Guid Value) : TypedId<Guid>(Value)
{
    public static ProjectTaskId New() => new(Guid.NewGuid());
}
