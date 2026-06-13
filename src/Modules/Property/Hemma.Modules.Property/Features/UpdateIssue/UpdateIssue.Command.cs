namespace Hemma.Modules.Property.Features.UpdateIssue;

public sealed record UpdateIssueCommand(
    Guid IssueId,
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string? Severity,
    DateOnly? DueDate,
    string? Notes);
