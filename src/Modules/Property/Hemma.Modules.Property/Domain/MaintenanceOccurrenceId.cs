using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Property.Domain;

public sealed record MaintenanceOccurrenceId(Guid Value) : TypedId<Guid>(Value)
{
    public static MaintenanceOccurrenceId New() => new(Guid.NewGuid());
}
