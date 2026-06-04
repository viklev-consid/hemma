namespace Hemma.Modules.Audit.Contracts.Queries;

public sealed record ListHouseholdAuditEntriesQuery(Guid HouseholdId, int Page = 1, int PageSize = 20);

public sealed record ListHouseholdAuditEntriesResponse(
    IReadOnlyList<HouseholdAuditEntryDto> Entries,
    int Total,
    int Page,
    int PageSize);

public sealed record HouseholdAuditEntryDto(
    Guid Id,
    string EventType,
    Guid? ActorId,
    string? ResourceType,
    Guid? ResourceId,
    DateTimeOffset OccurredAt,
    string Payload);
