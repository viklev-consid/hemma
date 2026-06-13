using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Property.Domain;

public sealed class PropertyIssue : AggregateRoot<PropertyIssueId>
{
    private PropertyIssue(
        PropertyIssueId id,
        Guid householdId,
        string title,
        string? description,
        PropertyAreaId? areaId,
        PropertyIssueSeverity severity,
        DateTimeOffset reportedAt,
        DateOnly? dueDate,
        string? notes) : base(id)
    {
        HouseholdId = householdId;
        Title = title;
        Description = description;
        AreaId = areaId;
        Severity = severity;
        Status = PropertyIssueStatus.Open;
        ReportedAt = reportedAt;
        DueDate = dueDate;
        Notes = notes;
    }

    private PropertyIssue() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PropertyAreaId? AreaId { get; private set; }
    public PropertyIssueSeverity Severity { get; private set; }
    public PropertyIssueStatus Status { get; private set; }
    public DateTimeOffset ReportedAt { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid? LinkedProjectId { get; private set; }
    public Guid? LinkedMaintenancePlanId { get; private set; }
    public Guid? LinkedMaintenanceOccurrenceId { get; private set; }
    public string? Notes { get; private set; }

    public static ErrorOr<PropertyIssue> Report(
        Guid householdId,
        string title,
        string? description,
        PropertyAreaId? areaId,
        PropertyIssueSeverity severity,
        DateOnly? dueDate,
        string? notes,
        IClock clock)
    {
        var details = ValidateDetails(title, description, notes);
        if (details.IsError)
        {
            return details.Errors;
        }

        if (!Enum.IsDefined(severity))
        {
            return PropertyErrors.IssueSeverityInvalid;
        }

        return new PropertyIssue(
            PropertyIssueId.New(),
            householdId,
            details.Value.Title,
            details.Value.Description,
            areaId,
            severity,
            clock.UtcNow,
            dueDate,
            details.Value.Notes);
    }

    public ErrorOr<Success> Update(
        string title,
        string? description,
        PropertyAreaId? areaId,
        PropertyIssueSeverity severity,
        DateOnly? dueDate,
        string? notes)
    {
        var details = ValidateDetails(title, description, notes);
        if (details.IsError)
        {
            return details.Errors;
        }

        if (!Enum.IsDefined(severity))
        {
            return PropertyErrors.IssueSeverityInvalid;
        }

        Title = details.Value.Title;
        Description = details.Value.Description;
        AreaId = areaId;
        Severity = severity;
        DueDate = dueDate;
        Notes = details.Value.Notes;
        return Result.Success;
    }

    public ErrorOr<Success> ChangeStatus(PropertyIssueStatus status, IClock clock)
    {
        if (!Enum.IsDefined(status))
        {
            return PropertyErrors.IssueStatusInvalid;
        }

        Status = status;
        ResolvedAt = status == PropertyIssueStatus.Resolved ? clock.UtcNow : null;
        ClosedAt = status == PropertyIssueStatus.Closed ? clock.UtcNow : null;
        return Result.Success;
    }

    public void LinkProject(Guid projectId)
    {
        LinkedProjectId = projectId;
        LinkedMaintenancePlanId = null;
        LinkedMaintenanceOccurrenceId = null;
    }

    public void LinkMaintenancePlan(Guid planId)
    {
        LinkedMaintenancePlanId = planId;
        LinkedProjectId = null;
        LinkedMaintenanceOccurrenceId = null;
    }

    public void LinkMaintenanceOccurrence(Guid occurrenceId)
    {
        LinkedMaintenanceOccurrenceId = occurrenceId;
        LinkedProjectId = null;
        LinkedMaintenancePlanId = null;
    }

    public void Unlink()
    {
        LinkedProjectId = null;
        LinkedMaintenancePlanId = null;
        LinkedMaintenanceOccurrenceId = null;
    }

    public void PromoteToProject(Guid projectId)
    {
        LinkProject(projectId);
        Status = PropertyIssueStatus.InProgress;
        ResolvedAt = null;
        ClosedAt = null;
    }

    public void CloseFromProject(IClock clock)
    {
        Status = PropertyIssueStatus.Closed;
        ClosedAt = clock.UtcNow;
        ResolvedAt = null;
    }

    private static ErrorOr<IssueDetails> ValidateDetails(string title, string? description, string? notes)
    {
        var normalizedTitle = title.Trim();
        if (normalizedTitle.Length is 0 or > 160)
        {
            return PropertyErrors.IssueTitleInvalid;
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription is { Length: > 2000 })
        {
            return PropertyErrors.IssueDescriptionInvalid;
        }

        var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (normalizedNotes is { Length: > 4000 })
        {
            return PropertyErrors.IssueNotesInvalid;
        }

        return new IssueDetails(normalizedTitle, normalizedDescription, normalizedNotes);
    }

    private sealed record IssueDetails(string Title, string? Description, string? Notes);
}
