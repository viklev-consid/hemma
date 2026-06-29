using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Property.Domain;

public sealed record ProjectAttachmentId(Guid Value) : TypedId<Guid>(Value)
{
    public static ProjectAttachmentId New() => new(Guid.NewGuid());
}
