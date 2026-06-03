using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Users.Domain;

public sealed record TermsAcceptanceId(Guid Value) : TypedId<Guid>(Value)
{
    public static TermsAcceptanceId New() => new(Guid.NewGuid());
}
