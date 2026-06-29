using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Property.Domain;

public sealed record MaintenancePlanId(Guid Value) : TypedId<Guid>(Value)
{
    public static MaintenancePlanId New() => new(Guid.NewGuid());
}
