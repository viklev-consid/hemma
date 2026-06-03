using Hemma.Modules.Audit.Contracts.Dtos;

namespace Hemma.Modules.Audit.Contracts.Queries;

public sealed record GetAuditTrailQuery(Guid UserId, int Page = 1, int PageSize = 20, string? EventType = null);

public sealed record GetAuditTrailResponse(
    IReadOnlyList<AuditEntryDto> Entries,
    int Total,
    int Page,
    int PageSize);
