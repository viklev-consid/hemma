using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Economy.Domain;

public sealed record EconomySettingsId(Guid Value) : TypedId<Guid>(Value)
{
    public static EconomySettingsId New() => new(Guid.NewGuid());
}
