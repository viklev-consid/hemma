using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Audit.Domain;

public sealed record AuditEntryId(Guid Value) : TypedId<Guid>(Value);
