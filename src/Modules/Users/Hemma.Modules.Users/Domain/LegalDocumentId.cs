using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Users.Domain;

public sealed record LegalDocumentId(Guid Value) : TypedId<Guid>(Value)
{
    public static LegalDocumentId New() => new(Guid.NewGuid());
}
