using Hemma.Modules.Audit.Contracts.Queries;

namespace Hemma.Modules.Households.Features.GetHouseholdAudit;

public sealed record GetHouseholdAuditResponse(
    Guid HouseholdId,
    string AccessMode,
    IReadOnlyList<HouseholdAuditEntryDto> Entries,
    int Total,
    int Page,
    int PageSize);
