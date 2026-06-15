using Hemma.Modules.Property.Domain;
using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.Shared;

public sealed record PropertyAreaResponse(
    Guid AreaId,
    Guid HouseholdId,
    string Name,
    string? Description,
    int SortOrder,
    bool IsArchived)
{
    public static PropertyAreaResponse FromArea(PropertyArea area) =>
        new(area.Id.Value, area.HouseholdId, area.Name, area.Description, area.SortOrder, area.IsArchived);
}

public sealed record ListAreasResponse(IReadOnlyList<PropertyAreaResponse> Areas);

public sealed record PropertyTagResponse(Guid TagId, Guid HouseholdId, string Name, string? Color, bool IsArchived)
{
    public static PropertyTagResponse FromTag(PropertyTag tag) =>
        new(tag.Id.Value, tag.HouseholdId, tag.Name, tag.Color, tag.IsArchived);
}

public sealed record ListTagsResponse(IReadOnlyList<PropertyTagResponse> Tags);

public sealed record AssignTagsResponse(string TargetType, Guid TargetId, IReadOnlyList<PropertyTagResponse> Tags);

public sealed record IssueResponse(
    Guid IssueId,
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string? AreaName,
    string Severity,
    string Status,
    DateTimeOffset ReportedAt,
    DateOnly? DueDate,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt,
    Guid? LinkedProjectId,
    Guid? LinkedMaintenancePlanId,
    Guid? LinkedMaintenanceOccurrenceId,
    string? Notes,
    bool IsOverdue,
    DateOnly? OverdueSince,
    int DaysOverdue)
{
    public static IssueResponse FromIssue(PropertyIssue issue, DateOnly? today = null)
    {
        var overdue = OverdueState.ForIssue(issue, ResponseClock.ResolveToday(today));
        return new(
            issue.Id.Value,
            issue.HouseholdId,
            issue.Title,
            issue.Description,
            issue.AreaId?.Value,
            null,
            issue.Severity.ToString(),
            issue.Status.ToString(),
            issue.ReportedAt,
            issue.DueDate,
            issue.ResolvedAt,
            issue.ClosedAt,
            issue.LinkedProjectId,
            issue.LinkedMaintenancePlanId,
            issue.LinkedMaintenanceOccurrenceId,
            issue.Notes,
            overdue.IsOverdue,
            overdue.OverdueSince,
            overdue.DaysOverdue);
    }
}

public sealed record ListIssuesResponse(IReadOnlyList<IssueResponse> Issues);

public sealed record PromoteIssueToProjectResponse(IssueResponse Issue, ProjectResponse Project);

public sealed record HistoryEntryResponse(
    Guid HistoryEntryId,
    Guid HouseholdId,
    DateOnly Date,
    string Title,
    Guid? AreaId,
    string? AreaName,
    MoneyDto? Cost,
    string Type,
    Guid? SourceProjectId,
    Guid? SourceMaintenanceOccurrenceId,
    IReadOnlyList<HistoryPhotoResponse> Photos)
{
    public static HistoryEntryResponse FromEntry(HistoryEntry entry) =>
        new(
            entry.Id.Value,
            entry.HouseholdId,
            entry.Date,
            entry.Title,
            entry.AreaId?.Value,
            null,
            entry.Cost is null ? null : new MoneyDto(entry.Cost.Amount, entry.Cost.Currency),
            entry.Type.ToString(),
            entry.SourceProjectId,
            entry.SourceMaintenanceOccurrenceId,
            entry.Photos.Select(HistoryPhotoResponse.FromPhoto).ToArray());
}

public sealed record HistoryPhotoResponse(string Container, string Key, string FileName, string ContentType, long Size)
{
    public static HistoryPhotoResponse FromPhoto(HistoryEntryPhoto photo) =>
        new(photo.BlobContainer, photo.BlobKey, photo.FileName, photo.ContentType, photo.Size);
}

public sealed record ListHistoryResponse(IReadOnlyList<HistoryEntryResponse> Entries);

public sealed record HistoryPhotoContentResponse(Stream Content, string ContentType, string FileName);

public sealed record MaintenancePlanResponse(
    Guid PlanId,
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string? AreaName,
    string RecurrenceUnit,
    int RecurrenceInterval,
    DateOnly AnchorDate,
    int LeadTimeDays,
    bool IsActive)
{
    public static MaintenancePlanResponse FromPlan(MaintenancePlan plan) =>
        new(
            plan.Id.Value,
            plan.HouseholdId,
            plan.Title,
            plan.Description,
            plan.AreaId?.Value,
            null,
            plan.RecurrenceUnit.ToString(),
            plan.RecurrenceInterval,
            plan.AnchorDate,
            plan.LeadTimeDays,
            plan.IsActive);
}

public sealed record ListMaintenancePlansResponse(IReadOnlyList<MaintenancePlanResponse> Plans);

public sealed record MaintenanceOccurrenceResponse(
    Guid OccurrenceId,
    Guid PlanId,
    Guid HouseholdId,
    DateOnly DueDate,
    DateOnly OriginalDueDate,
    DateOnly EffectiveReminderDate,
    DateOnly? SnoozedUntil,
    DateTimeOffset? SnoozedAt,
    string? SnoozeReason,
    string Status,
    DateTimeOffset? CompletedAt,
    string? Notes,
    Guid? SpawnedProjectId,
    bool IsOverdue,
    DateOnly? OverdueSince,
    int DaysOverdue)
{
    public static MaintenanceOccurrenceResponse FromOccurrence(MaintenanceOccurrence occurrence, DateOnly? today = null)
    {
        var overdue = OverdueState.ForMaintenanceOccurrence(occurrence, ResponseClock.ResolveToday(today));
        return new(
            occurrence.Id.Value,
            occurrence.PlanId.Value,
            occurrence.HouseholdId,
            occurrence.DueDate,
            occurrence.OriginalDueDate,
            occurrence.SnoozedUntil ?? occurrence.DueDate,
            occurrence.SnoozedUntil,
            occurrence.SnoozedAt,
            occurrence.SnoozeReason,
            occurrence.Status.ToString(),
            occurrence.CompletedAt,
            occurrence.Notes,
            occurrence.SpawnedProjectId,
            overdue.IsOverdue,
            overdue.OverdueSince,
            overdue.DaysOverdue);
    }
}

public sealed record GetMaintenancePlanResponse(
    MaintenancePlanResponse Plan,
    MaintenanceOccurrenceResponse? NextOccurrence);

public sealed record UpcomingOccurrenceItem(
    Guid OccurrenceId,
    Guid PlanId,
    Guid HouseholdId,
    string PlanTitle,
    Guid? AreaId,
    string? AreaName,
    DateOnly DueDate,
    DateOnly OriginalDueDate,
    DateOnly EffectiveReminderDate,
    DateOnly? SnoozedUntil,
    DateTimeOffset? SnoozedAt,
    string? SnoozeReason,
    string Status,
    bool IsOverdue,
    DateOnly? OverdueSince,
    int DaysOverdue);

public sealed record ListUpcomingOccurrencesResponse(IReadOnlyList<UpcomingOccurrenceItem> Occurrences);

public sealed record CompleteOccurrenceResponse(
    MaintenanceOccurrenceResponse Occurrence,
    SuggestedHistoryEntryResponse? SuggestedHistoryEntry,
    MaintenanceOccurrenceResponse? NextOccurrence);

public sealed record SkipOccurrenceResponse(
    MaintenanceOccurrenceResponse Occurrence,
    MaintenanceOccurrenceResponse? NextOccurrence);

public sealed record PromoteOccurrenceResponse(
    MaintenanceOccurrenceResponse Occurrence,
    ProjectResponse Project,
    MaintenanceOccurrenceResponse? NextOccurrence);

public sealed record ProjectResponse(
    Guid ProjectId,
    Guid HouseholdId,
    string Name,
    string? Description,
    string Status,
    Guid? AreaId,
    string? AreaName,
    string Priority,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate,
    MoneyDto? BudgetEstimate,
    DateTimeOffset? CompletedAt,
    string? Notes,
    bool IsOverdue,
    DateOnly? OverdueSince,
    int DaysOverdue,
    IReadOnlyList<ProjectTaskResponse> Tasks,
    IReadOnlyList<ProjectLinkResponse> Links,
    IReadOnlyList<ProjectAttachmentResponse> Attachments)
{
    public static ProjectResponse FromProject(Project project, DateOnly? today = null)
    {
        var current = ResponseClock.ResolveToday(today);
        var overdue = OverdueState.ForProject(project, current);
        return new(
            project.Id.Value,
            project.HouseholdId,
            project.Name,
            project.Description,
            project.Status.ToString(),
            project.AreaId?.Value,
            null,
            project.Priority.ToString(),
            project.TargetStartDate,
            project.TargetEndDate,
            project.BudgetEstimate is null ? null : new MoneyDto(project.BudgetEstimate.Amount, project.BudgetEstimate.Currency),
            project.CompletedAt,
            project.Notes,
            overdue.IsOverdue,
            overdue.OverdueSince,
            overdue.DaysOverdue,
            project.Tasks.OrderBy(task => task.SortOrder).Select(task => ProjectTaskResponse.FromTask(task, current)).ToArray(),
            project.Links.Select(ProjectLinkResponse.FromLink).ToArray(),
            project.Attachments.Select(ProjectAttachmentResponse.FromAttachment).ToArray());
    }
}

public sealed record ProjectListItemResponse(
    Guid ProjectId,
    Guid HouseholdId,
    string Name,
    string? Description,
    string Status,
    Guid? AreaId,
    string? AreaName,
    string Priority,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate,
    MoneyDto? BudgetEstimate,
    DateTimeOffset? CompletedAt,
    string? Notes,
    bool IsOverdue,
    DateOnly? OverdueSince,
    int DaysOverdue);

public sealed record ListProjectsResponse(IReadOnlyList<ProjectListItemResponse> Projects);

public sealed record ProjectTaskResponse(
    Guid TaskId,
    Guid ProjectId,
    string Title,
    string Status,
    MoneyDto? Estimate,
    Guid? AssigneeId,
    DateOnly? DueDate,
    int SortOrder,
    bool IsOverdue,
    DateOnly? OverdueSince,
    int DaysOverdue)
{
    public static ProjectTaskResponse FromTask(ProjectTask task, DateOnly? today = null)
    {
        var overdue = OverdueState.ForProjectTask(task, ResponseClock.ResolveToday(today));
        return new(
            task.Id.Value,
            task.ProjectId.Value,
            task.Title,
            task.Status.ToString(),
            task.Estimate is null ? null : new MoneyDto(task.Estimate.Amount, task.Estimate.Currency),
            task.AssigneeId,
            task.DueDate,
            task.SortOrder,
            overdue.IsOverdue,
            overdue.OverdueSince,
            overdue.DaysOverdue);
    }
}

public sealed record GetProjectTasksResponse(IReadOnlyList<ProjectTaskResponse> Tasks);

public sealed record ProjectLinkResponse(Guid LinkId, Guid ProjectId, string Label, string Url)
{
    public static ProjectLinkResponse FromLink(ProjectLink link) =>
        new(link.Id.Value, link.ProjectId.Value, link.Label, link.Url);
}

public sealed record ProjectAttachmentResponse(Guid AttachmentId, Guid ProjectId, string FileName, string ContentType, long Size)
{
    public static ProjectAttachmentResponse FromAttachment(ProjectAttachment attachment) =>
        new(attachment.Id.Value, attachment.ProjectId.Value, attachment.FileName, attachment.ContentType, attachment.Size);
}

public sealed record SuggestedHistoryAttachmentResponse(string Container, string Key);

public sealed record SuggestedHistoryEntryResponse(
    DateOnly Date,
    string Title,
    Guid? AreaId,
    string? AreaName,
    MoneyDto? Cost,
    string Type,
    Guid? SourceProjectId,
    Guid? SourceMaintenanceOccurrenceId,
    IReadOnlyList<SuggestedHistoryAttachmentResponse> PhotoRefs);

public sealed record ChangeProjectStatusResponse(ProjectResponse Project, SuggestedHistoryEntryResponse? SuggestedHistoryEntry);

public sealed record AttachmentContentResponse(Stream Content, string ContentType, string FileName);

public sealed record GetProjectBudgetResponse(
    MoneyDto? Estimate,
    MoneyDto LinkedTotal,
    MoneyDto? Remaining,
    int TransactionCount);

public sealed record HistoryPhotoRefRequest(string Container, string Key);

internal sealed record OverdueState(bool IsOverdue, DateOnly? OverdueSince, int DaysOverdue)
{
    public static OverdueState ForMaintenanceOccurrence(MaintenanceOccurrence occurrence, DateOnly today) =>
        occurrence.Status == MaintenanceOccurrenceStatus.Upcoming && (occurrence.SnoozedUntil ?? occurrence.DueDate) < today
            ? Create(occurrence.SnoozedUntil ?? occurrence.DueDate, today)
            : None;

    public static OverdueState ForProject(Project project, DateOnly today) =>
        project.Status != ProjectStatus.Done && project.TargetEndDate is { } dueDate && dueDate < today
            ? Create(dueDate, today)
            : None;

    public static OverdueState ForProjectTask(ProjectTask task, DateOnly today) =>
        task.Status != ProjectTaskStatus.Done && task.DueDate is { } dueDate && dueDate < today
            ? Create(dueDate, today)
            : None;

    public static OverdueState ForIssue(PropertyIssue issue, DateOnly today) =>
        (issue.Status == PropertyIssueStatus.Open || issue.Status == PropertyIssueStatus.InProgress)
            && issue.DueDate is { } dueDate
            && dueDate < today
                ? Create(dueDate, today)
                : None;

    private static OverdueState None => new(false, null, 0);

    private static OverdueState Create(DateOnly dueDate, DateOnly today) =>
        new(true, dueDate, today.DayNumber - dueDate.DayNumber);
}

internal static class ResponseClock
{
    public static DateOnly ResolveToday(DateOnly? today) =>
        today ?? DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
}
