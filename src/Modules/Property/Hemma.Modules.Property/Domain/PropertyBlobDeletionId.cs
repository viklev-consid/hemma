using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Property.Domain;

public sealed record PropertyBlobDeletionId(Guid Value) : TypedId<Guid>(Value)
{
    public static PropertyBlobDeletionId New() => new(Guid.NewGuid());
}
