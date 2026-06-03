using Hemma.Modules.Audit.Contracts.Queries;

namespace Hemma.Modules.Organizations.Features.GetOrganizationAudit;

public sealed record GetOrganizationAuditResponse(
    Guid OrganizationId,
    string AccessMode,
    IReadOnlyList<OrganizationAuditEntryDto> Entries,
    int Total,
    int Page,
    int PageSize);
