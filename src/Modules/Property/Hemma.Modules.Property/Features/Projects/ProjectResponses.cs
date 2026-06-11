using Hemma.Modules.Property.Domain;
using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.Projects;

public sealed record ProjectResponse(
    Guid ProjectId,
    Guid HouseholdId,
    string Name,
    string? Description,
    string Status,
    string? Area,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate,
    MoneyDto? BudgetEstimate,
    DateTimeOffset? CompletedAt,
    string? Notes,
    IReadOnlyList<ProjectTaskResponse> Tasks,
    IReadOnlyList<ProjectLinkResponse> Links,
    IReadOnlyList<ProjectAttachmentResponse> Attachments)
{
    public static ProjectResponse FromProject(Project project) =>
        new(
            project.Id.Value,
            project.HouseholdId,
            project.Name,
            project.Description,
            project.Status.ToString(),
            project.Area,
            project.TargetStartDate,
            project.TargetEndDate,
            project.BudgetEstimate is null ? null : new MoneyDto(project.BudgetEstimate.Amount, project.BudgetEstimate.Currency),
            project.CompletedAt,
            project.Notes,
            project.Tasks.OrderBy(task => task.SortOrder).Select(ProjectTaskResponse.FromTask).ToArray(),
            project.Links.Select(ProjectLinkResponse.FromLink).ToArray(),
            project.Attachments.Select(ProjectAttachmentResponse.FromAttachment).ToArray());
}

public sealed record ProjectListItemResponse(
    Guid ProjectId,
    Guid HouseholdId,
    string Name,
    string? Description,
    string Status,
    string? Area,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate,
    MoneyDto? BudgetEstimate,
    DateTimeOffset? CompletedAt,
    string? Notes);

public sealed record ListProjectsResponse(IReadOnlyList<ProjectListItemResponse> Projects);

public sealed record ProjectTaskResponse(
    Guid TaskId,
    Guid ProjectId,
    string Title,
    string Status,
    MoneyDto? Estimate,
    Guid? AssigneeId,
    DateOnly? DueDate,
    int SortOrder)
{
    public static ProjectTaskResponse FromTask(ProjectTask task) =>
        new(
            task.Id.Value,
            task.ProjectId.Value,
            task.Title,
            task.Status.ToString(),
            task.Estimate is null ? null : new MoneyDto(task.Estimate.Amount, task.Estimate.Currency),
            task.AssigneeId,
            task.DueDate,
            task.SortOrder);
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

public sealed record SuggestedHistoryAttachmentResponse(Guid AttachmentId, string FileName, string ContentType);

public sealed record SuggestedHistoryEntryResponse(
    DateOnly Date,
    string Title,
    string? Area,
    MoneyDto? Cost,
    string Type,
    Guid? SourceProjectId,
    Guid? SourceMaintenanceOccurrenceId,
    IReadOnlyList<SuggestedHistoryAttachmentResponse> PhotoRefs);

public sealed record ChangeProjectStatusResponse(ProjectResponse Project, SuggestedHistoryEntryResponse? SuggestedHistoryEntry);

public sealed record AttachmentContentResponse(Stream Content, string ContentType, string FileName);
