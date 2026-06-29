namespace Hemma.Modules.Property.Features.ReportIssue;

public sealed record IssueRequest(
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string? Severity,
    DateOnly? DueDate,
    string? Notes);
