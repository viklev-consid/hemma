namespace Hemma.Modules.Property.Features.ReportIssue;

public sealed record ReportIssueCommand(
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string? Severity,
    DateOnly? DueDate,
    string? Notes);
