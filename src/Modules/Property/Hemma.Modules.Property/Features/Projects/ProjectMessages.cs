using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.Projects;

public sealed record CreateProjectCommand(
    Guid HouseholdId,
    string Name,
    string? Description,
    string Status,
    Guid? AreaId,
    string? Priority,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate,
    MoneyDto? BudgetEstimate,
    string? Notes);

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    Guid HouseholdId,
    string Name,
    string? Description,
    Guid? AreaId,
    string? Priority,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate,
    MoneyDto? BudgetEstimate,
    string? Notes);

public sealed record ChangeProjectStatusCommand(Guid ProjectId, Guid HouseholdId, string Status);

public sealed record DeleteProjectCommand(Guid ProjectId, Guid HouseholdId);

public sealed record AddTaskCommand(Guid ProjectId, Guid HouseholdId, string Title, string Status, MoneyDto? Estimate, Guid? AssigneeId, DateOnly? DueDate);

public sealed record UpdateTaskCommand(Guid ProjectId, Guid TaskId, Guid HouseholdId, string Title, string Status, MoneyDto? Estimate, Guid? AssigneeId, DateOnly? DueDate);

public sealed record DeleteTaskCommand(Guid ProjectId, Guid TaskId, Guid HouseholdId);

public sealed record ReorderTasksCommand(Guid ProjectId, Guid HouseholdId, IReadOnlyList<Guid> TaskIds);

public sealed record AddLinkCommand(Guid ProjectId, Guid HouseholdId, string Label, string Url);

public sealed record RemoveLinkCommand(Guid ProjectId, Guid LinkId, Guid HouseholdId);

public sealed record AddAttachmentCommand(Guid ProjectId, Guid HouseholdId, string FileName, string ContentType, byte[] Content);

public sealed record RemoveAttachmentCommand(Guid ProjectId, Guid AttachmentId, Guid HouseholdId);

public sealed record GetProjectQuery(Guid ProjectId, Guid HouseholdId);

public sealed record ListProjectsQuery(Guid HouseholdId, string? Status, Guid? AreaId, string? Priority, IReadOnlyList<Guid>? TagIds);

public sealed record GetProjectTasksQuery(Guid ProjectId, Guid HouseholdId);

public sealed record GetAttachmentContentQuery(Guid ProjectId, Guid AttachmentId, Guid HouseholdId);

public sealed record GetProjectBudgetQuery(Guid ProjectId, Guid HouseholdId);
