using ErrorOr;
using Hemma.Modules.Property.Contracts.Events;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Modules.Property.Integration;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Contracts;
using Hemma.Shared.Infrastructure.Blobs;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hemma.Modules.Property.Features.Projects;

public sealed class ProjectHandler(
    PropertyDbContext db,
    IBlobStore blobStore,
    IMessageBus bus,
    PropertyAuditPublisher audit,
    IClock clock)
{
    public async Task<ErrorOr<ProjectResponse>> Handle(CreateProjectCommand cmd, CancellationToken ct)
    {
        var status = ParseProjectStatus(cmd.Status);
        if (status is null)
        {
            return PropertyErrors.ProjectStatusInvalid;
        }

        var estimate = ToMoney(cmd.BudgetEstimate);
        if (estimate.IsError)
        {
            return estimate.Errors;
        }

        var project = Project.Create(
            cmd.HouseholdId,
            cmd.Name,
            cmd.Description,
            status.Value,
            cmd.Area,
            cmd.TargetStartDate,
            cmd.TargetEndDate,
            estimate.Value.Value,
            cmd.Notes);
        if (project.IsError)
        {
            return project.Errors;
        }

        db.Projects.Add(project.Value);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.created", "Project", project.Value.Id.Value, null, ct);
        return ProjectResponse.FromProject(project.Value);
    }

    public async Task<ErrorOr<ProjectResponse>> Handle(UpdateProjectCommand cmd, CancellationToken ct)
    {
        var project = await LoadProjectAsync(cmd.HouseholdId, cmd.ProjectId, ct);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var estimate = ToMoney(cmd.BudgetEstimate);
        if (estimate.IsError)
        {
            return estimate.Errors;
        }

        var updated = project.UpdateDetails(
            cmd.Name,
            cmd.Description,
            cmd.Area,
            cmd.TargetStartDate,
            cmd.TargetEndDate,
            estimate.Value.Value,
            cmd.Notes);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.updated", "Project", project.Id.Value, null, ct);
        return ProjectResponse.FromProject(project);
    }

    public async Task<ErrorOr<ChangeProjectStatusResponse>> Handle(ChangeProjectStatusCommand cmd, CancellationToken ct)
    {
        var status = ParseProjectStatus(cmd.Status);
        if (status is null)
        {
            return PropertyErrors.ProjectStatusInvalid;
        }

        var project = await LoadProjectAsync(cmd.HouseholdId, cmd.ProjectId, ct);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var changed = project.ChangeStatus(status.Value, clock);
        if (changed.IsError)
        {
            return changed.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.status_changed", "Project", project.Id.Value, null, ct);

        var suggested = status.Value == ProjectStatus.Done && project.CompletedAt is not null
            ? new SuggestedHistoryEntryResponse(
                DateOnly.FromDateTime(project.CompletedAt.Value.UtcDateTime),
                project.Name,
                project.Area,
                null,
                "Project",
                project.Id.Value,
                null,
                project.Attachments.Select(a => new SuggestedHistoryAttachmentResponse(a.Id.Value, a.FileName, a.ContentType)).ToArray())
            : null;

        return new ChangeProjectStatusResponse(ProjectResponse.FromProject(project), suggested);
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteProjectCommand cmd, CancellationToken ct)
    {
        var project = await LoadProjectAsync(cmd.HouseholdId, cmd.ProjectId, ct);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var attachments = project.Attachments
            .Select(attachment => new BlobRef(attachment.BlobContainer, attachment.BlobKey))
            .ToArray();

        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);

        foreach (var attachment in attachments)
        {
            await blobStore.DeleteAsync(attachment, ct);
        }

        await bus.PublishAsync(new ProjectDeletedV1(cmd.HouseholdId, cmd.ProjectId, Guid.NewGuid()));
        await audit.PublishAsync(cmd.HouseholdId, "property.project.deleted", "Project", cmd.ProjectId, null, ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<ProjectResponse>> Handle(GetProjectQuery query, CancellationToken ct)
    {
        var project = await LoadProjectAsync(query.HouseholdId, query.ProjectId, ct, tracking: false);
        return project is null ? PropertyErrors.ProjectNotFound : ProjectResponse.FromProject(project);
    }

    public async Task<ErrorOr<ListProjectsResponse>> Handle(ListProjectsQuery query, CancellationToken ct)
    {
        var projects = db.Projects.AsNoTracking().Where(project => project.HouseholdId == query.HouseholdId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ParseProjectStatus(query.Status);
            if (status is null)
            {
                return PropertyErrors.ProjectStatusInvalid;
            }

            projects = projects.Where(project => project.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Area))
        {
            projects = projects.Where(project => project.Area == query.Area);
        }

        var items = await projects
            .OrderBy(project => project.Status)
            .ThenBy(project => project.Name)
            .Select(project => new ProjectListItemResponse(
                project.Id.Value,
                project.HouseholdId,
                project.Name,
                project.Description,
                project.Status.ToString(),
                project.Area,
                project.TargetStartDate,
                project.TargetEndDate,
                project.BudgetEstimate == null ? null : new MoneyDto(project.BudgetEstimate.Amount, project.BudgetEstimate.Currency),
                project.CompletedAt,
                project.Notes))
            .ToArrayAsync(ct);

        return new ListProjectsResponse(items);
    }

    public async Task<ErrorOr<ProjectTaskResponse>> Handle(AddTaskCommand cmd, CancellationToken ct)
    {
        var project = await LoadProjectAsync(cmd.HouseholdId, cmd.ProjectId, ct);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var status = ParseTaskStatus(cmd.Status);
        if (status is null)
        {
            return PropertyErrors.TaskStatusInvalid;
        }

        var estimate = ToMoney(cmd.Estimate);
        if (estimate.IsError)
        {
            return estimate.Errors;
        }

        var task = project.AddTask(cmd.Title, status.Value, estimate.Value.Value, cmd.AssigneeId, cmd.DueDate);
        if (task.IsError)
        {
            return task.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.task_added", "ProjectTask", task.Value.Id.Value, null, ct);
        return ProjectTaskResponse.FromTask(task.Value);
    }

    public async Task<ErrorOr<ProjectTaskResponse>> Handle(UpdateTaskCommand cmd, CancellationToken ct)
    {
        var project = await LoadProjectAsync(cmd.HouseholdId, cmd.ProjectId, ct);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var status = ParseTaskStatus(cmd.Status);
        if (status is null)
        {
            return PropertyErrors.TaskStatusInvalid;
        }

        var estimate = ToMoney(cmd.Estimate);
        if (estimate.IsError)
        {
            return estimate.Errors;
        }

        var taskId = new ProjectTaskId(cmd.TaskId);
        var updated = project.UpdateTask(taskId, cmd.Title, status.Value, estimate.Value.Value, cmd.AssigneeId, cmd.DueDate);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.task_updated", "ProjectTask", cmd.TaskId, null, ct);
        return ProjectTaskResponse.FromTask(project.Tasks.Single(task => task.Id == taskId));
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteTaskCommand cmd, CancellationToken ct)
    {
        var project = await LoadProjectAsync(cmd.HouseholdId, cmd.ProjectId, ct);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var removed = project.RemoveTask(new ProjectTaskId(cmd.TaskId));
        if (removed.IsError)
        {
            return removed.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.task_deleted", "ProjectTask", cmd.TaskId, null, ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<GetProjectTasksResponse>> Handle(GetProjectTasksQuery query, CancellationToken ct)
    {
        var project = await LoadProjectAsync(query.HouseholdId, query.ProjectId, ct, tracking: false);
        return project is null
            ? PropertyErrors.ProjectNotFound
            : new GetProjectTasksResponse(project.Tasks.OrderBy(task => task.SortOrder).Select(ProjectTaskResponse.FromTask).ToArray());
    }

    public async Task<ErrorOr<GetProjectTasksResponse>> Handle(ReorderTasksCommand cmd, CancellationToken ct)
    {
        var project = await LoadProjectAsync(cmd.HouseholdId, cmd.ProjectId, ct);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var result = project.ReorderTasks(cmd.TaskIds.Select(id => new ProjectTaskId(id)).ToArray());
        if (result.IsError)
        {
            return result.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.tasks_reordered", "Project", cmd.ProjectId, null, ct);
        return new GetProjectTasksResponse(project.Tasks.OrderBy(task => task.SortOrder).Select(ProjectTaskResponse.FromTask).ToArray());
    }

    public async Task<ErrorOr<ProjectLinkResponse>> Handle(AddLinkCommand cmd, CancellationToken ct)
    {
        var project = await LoadProjectAsync(cmd.HouseholdId, cmd.ProjectId, ct);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var link = project.AddLink(cmd.Label, cmd.Url);
        if (link.IsError)
        {
            return link.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.link_added", "ProjectLink", link.Value.Id.Value, null, ct);
        return ProjectLinkResponse.FromLink(link.Value);
    }

    public async Task<ErrorOr<Deleted>> Handle(RemoveLinkCommand cmd, CancellationToken ct)
    {
        var project = await LoadProjectAsync(cmd.HouseholdId, cmd.ProjectId, ct);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var removed = project.RemoveLink(new ProjectLinkId(cmd.LinkId));
        if (removed.IsError)
        {
            return removed.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.link_removed", "ProjectLink", cmd.LinkId, null, ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<ProjectAttachmentResponse>> Handle(AddAttachmentCommand cmd, CancellationToken ct)
    {
        if (!ProjectAttachmentRules.IsAllowed(cmd.ContentType, cmd.Content.LongLength))
        {
            return PropertyErrors.AttachmentFileInvalid;
        }

        var project = await LoadProjectAsync(cmd.HouseholdId, cmd.ProjectId, ct);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        await using var stream = new MemoryStream(cmd.Content, writable: false);
        var blobRef = await blobStore.PutAsync(stream, new BlobMetadata(cmd.ContentType, cmd.Content.LongLength, cmd.FileName), ct);

        var attachment = project.AddAttachment(blobRef.Container, blobRef.Key, cmd.FileName, cmd.ContentType, cmd.Content.LongLength);
        if (attachment.IsError)
        {
            await blobStore.DeleteAsync(blobRef, ct);
            return attachment.Errors;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await blobStore.DeleteAsync(blobRef, ct);
            throw;
        }

        await audit.PublishAsync(cmd.HouseholdId, "property.project.attachment_added", "ProjectAttachment", attachment.Value.Id.Value, null, ct);
        return ProjectAttachmentResponse.FromAttachment(attachment.Value);
    }

    public async Task<ErrorOr<AttachmentContentResponse>> Handle(GetAttachmentContentQuery query, CancellationToken ct)
    {
        var attachmentId = new ProjectAttachmentId(query.AttachmentId);
        var attachment = await db.Projects
            .AsNoTracking()
            .Where(project => project.HouseholdId == query.HouseholdId && project.Id == new ProjectId(query.ProjectId))
            .SelectMany(project => project.Attachments)
            .SingleOrDefaultAsync(attachment => attachment.Id == attachmentId, ct);
        if (attachment is null)
        {
            return PropertyErrors.AttachmentNotFound;
        }

        var content = await blobStore.GetAsync(new BlobRef(attachment.BlobContainer, attachment.BlobKey), ct);
        return new AttachmentContentResponse(content.Stream, attachment.ContentType, attachment.FileName);
    }

    public async Task<ErrorOr<Deleted>> Handle(RemoveAttachmentCommand cmd, CancellationToken ct)
    {
        var project = await LoadProjectAsync(cmd.HouseholdId, cmd.ProjectId, ct);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var attachment = project.RemoveAttachment(new ProjectAttachmentId(cmd.AttachmentId));
        if (attachment.IsError)
        {
            return attachment.Errors;
        }

        await db.SaveChangesAsync(ct);
        await blobStore.DeleteAsync(new BlobRef(attachment.Value.BlobContainer, attachment.Value.BlobKey), ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.attachment_removed", "ProjectAttachment", cmd.AttachmentId, null, ct);
        return Result.Deleted;
    }

    private async Task<Project?> LoadProjectAsync(Guid householdId, Guid projectId, CancellationToken ct, bool tracking = true)
    {
        var query = db.Projects
            .Include(project => project.Tasks)
            .Include(project => project.Links)
            .Include(project => project.Attachments)
            .AsSplitQuery()
            .Where(project => project.HouseholdId == householdId && project.Id == new ProjectId(projectId));

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(ct);
    }

    private static ProjectStatus? ParseProjectStatus(string status) =>
        Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;

    private static ProjectTaskStatus? ParseTaskStatus(string status) =>
        Enum.TryParse<ProjectTaskStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;

    private static ErrorOr<OptionalMoney> ToMoney(MoneyDto? money)
    {
        if (money is null)
        {
            return new OptionalMoney(null);
        }

        var created = Money.Create(money.Amount, money.Currency);
        return created.IsError ? created.Errors : new OptionalMoney(created.Value);
    }

    private sealed record OptionalMoney(Money? Value);
}
