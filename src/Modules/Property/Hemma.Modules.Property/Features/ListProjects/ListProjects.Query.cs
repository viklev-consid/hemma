namespace Hemma.Modules.Property.Features.ListProjects;

public sealed record ListProjectsQuery(
    Guid HouseholdId,
    string? Status,
    Guid? AreaId,
    string? Priority,
    IReadOnlyList<Guid>? TagIds,
    bool? IsOverdue);
