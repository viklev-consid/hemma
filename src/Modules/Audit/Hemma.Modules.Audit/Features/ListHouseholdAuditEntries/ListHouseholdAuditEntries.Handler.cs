using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Audit.Contracts.Queries;
using Hemma.Modules.Audit.Errors;
using Hemma.Modules.Audit.Persistence;
using Hemma.Shared.Kernel.Pagination;

namespace Hemma.Modules.Audit.Features.ListHouseholdAuditEntries;

public sealed class ListHouseholdAuditEntriesHandler(AuditDbContext db)
{
    public async Task<ErrorOr<ListHouseholdAuditEntriesResponse>> Handle(ListHouseholdAuditEntriesQuery query, CancellationToken ct)
        => await AuditTelemetry.InstrumentAsync(nameof(ListHouseholdAuditEntriesHandler), () => HandleCoreAsync(query, ct));

    private async Task<ErrorOr<ListHouseholdAuditEntriesResponse>> HandleCoreAsync(ListHouseholdAuditEntriesQuery query, CancellationToken ct)
    {
        if (query.Page <= 0 || query.Page > PageRequest.MaxPage)
        {
            return AuditErrors.PageInvalid;
        }

        if (query.PageSize <= 0 || query.PageSize > PageRequest.MaxPageSize)
        {
            return AuditErrors.PageSizeInvalid;
        }

        var pagination = PageRequest.Of(query.Page, query.PageSize);

        var baseQuery = db.AuditEntries
            .AsNoTracking()
            .Where(e => e.HouseholdId == query.HouseholdId)
            .OrderByDescending(e => e.OccurredAt);

        var total = await baseQuery.CountAsync(ct);

        var entries = await baseQuery
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(e => new HouseholdAuditEntryDto(
                e.Id.Value,
                e.EventType,
                e.ActorId,
                e.ResourceType,
                e.ResourceId,
                e.OccurredAt,
                e.Payload))
            .ToListAsync(ct);

        return new ListHouseholdAuditEntriesResponse(entries, total, pagination.Page, pagination.PageSize);
    }
}
