using ErrorOr;
using Hemma.Modules.Economy.Contracts.Queries;
using Hemma.Modules.Property.Contracts.Events;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Modules.Property.Features.AddAttachment;
using Hemma.Modules.Property.Features.AddLink;
using Hemma.Modules.Property.Features.AddTask;
using Hemma.Modules.Property.Features.ArchiveArea;
using Hemma.Modules.Property.Features.ArchiveTag;
using Hemma.Modules.Property.Features.AssignTags;
using Hemma.Modules.Property.Features.ChangeIssueStatus;
using Hemma.Modules.Property.Features.ChangeProjectStatus;
using Hemma.Modules.Property.Features.CompleteOccurrence;
using Hemma.Modules.Property.Features.CreateArea;
using Hemma.Modules.Property.Features.CreateHistoryEntry;
using Hemma.Modules.Property.Features.CreateMaintenancePlan;
using Hemma.Modules.Property.Features.CreateProject;
using Hemma.Modules.Property.Features.CreateTag;
using Hemma.Modules.Property.Features.DeactivatePlan;
using Hemma.Modules.Property.Features.DeleteHistoryEntry;
using Hemma.Modules.Property.Features.DeleteIssue;
using Hemma.Modules.Property.Features.DeletePlan;
using Hemma.Modules.Property.Features.DeleteProject;
using Hemma.Modules.Property.Features.DeleteTask;
using Hemma.Modules.Property.Features.GetAttachmentContent;
using Hemma.Modules.Property.Features.GetHistoryPhoto;
using Hemma.Modules.Property.Features.GetIssue;
using Hemma.Modules.Property.Features.GetMaintenancePlan;
using Hemma.Modules.Property.Features.GetProject;
using Hemma.Modules.Property.Features.GetProjectBudget;
using Hemma.Modules.Property.Features.GetProjectTasks;
using Hemma.Modules.Property.Features.LinkIssueToMaintenanceOccurrence;
using Hemma.Modules.Property.Features.LinkIssueToMaintenancePlan;
using Hemma.Modules.Property.Features.ListAreas;
using Hemma.Modules.Property.Features.ListHistory;
using Hemma.Modules.Property.Features.ListIssues;
using Hemma.Modules.Property.Features.ListMaintenancePlans;
using Hemma.Modules.Property.Features.ListProjects;
using Hemma.Modules.Property.Features.ListTags;
using Hemma.Modules.Property.Features.ListUpcomingOccurrences;
using Hemma.Modules.Property.Features.PromoteIssueToProject;
using Hemma.Modules.Property.Features.PromoteOccurrenceToProject;
using Hemma.Modules.Property.Features.RemoveAttachment;
using Hemma.Modules.Property.Features.RemoveLink;
using Hemma.Modules.Property.Features.ReorderAreas;
using Hemma.Modules.Property.Features.ReorderTasks;
using Hemma.Modules.Property.Features.ReportIssue;
using Hemma.Modules.Property.Features.SkipOccurrence;
using Hemma.Modules.Property.Features.UnlinkIssue;
using Hemma.Modules.Property.Features.UpdateArea;
using Hemma.Modules.Property.Features.UpdateHistoryEntry;
using Hemma.Modules.Property.Features.UpdateIssue;
using Hemma.Modules.Property.Features.UpdateMaintenancePlan;
using Hemma.Modules.Property.Features.UpdateProject;
using Hemma.Modules.Property.Features.UpdateTag;
using Hemma.Modules.Property.Features.UpdateTask;
using Hemma.Modules.Property.Integration;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Contracts;
using Hemma.Shared.Infrastructure.Blobs;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hemma.Modules.Property.Features.Shared;

public sealed class ProjectsOperations(
    PropertyDbContext db,
    IBlobStore blobStore,
    IMessageBus bus,
    PropertyAuditPublisher audit,
    IClock clock,
    ActivityOperations activity)
{
    private const string defaultCurrency = "SEK";
    private const int maxListItems = 100;

    public async Task<ErrorOr<ProjectResponse>> CreateProjectAsync(CreateProjectCommand cmd, CancellationToken ct)
    {
        var status = ParseProjectStatus(cmd.Status);
        if (status is null)
        {
            return PropertyErrors.ProjectStatusInvalid;
        }

        var priority = ParseProjectPriority(cmd.Priority);
        if (priority is null)
        {
            return PropertyErrors.ProjectPriorityInvalid;
        }

        var areaId = await ValidateAreaAsync(cmd.HouseholdId, cmd.AreaId, ct);
        if (areaId.IsError)
        {
            return areaId.Errors;
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
            areaId.Value.Value,
            priority.Value,
            cmd.TargetStartDate,
            cmd.TargetEndDate,
            estimate.Value.Value,
            cmd.Notes);
        if (project.IsError)
        {
            return project.Errors;
        }

        db.Projects.Add(project.Value);
        var activityResult = activity.Append(
            cmd.HouseholdId,
            PropertyActivityVerb.ProjectCreated,
            PropertyActivityTargetType.Project,
            project.Value.Id.Value,
            $"Project \"{project.Value.Name}\" was created.",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["status"] = project.Value.Status.ToString(),
                ["priority"] = project.Value.Priority.ToString()
            });
        if (activityResult.IsError)
        {
            return activityResult.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.created", "Project", project.Value.Id.Value, null, ct);
        return await EnrichProjectAsync(ProjectResponse.FromProject(project.Value, Today), includeTags: false, ct);
    }

    public async Task<ErrorOr<ProjectResponse>> UpdateProjectAsync(UpdateProjectCommand cmd, CancellationToken ct)
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

        var priority = ParseProjectPriority(cmd.Priority);
        if (priority is null)
        {
            return PropertyErrors.ProjectPriorityInvalid;
        }

        var areaId = await ValidateAreaAsync(cmd.HouseholdId, cmd.AreaId, ct);
        if (areaId.IsError)
        {
            return areaId.Errors;
        }

        var updated = project.UpdateDetails(
            cmd.Name,
            cmd.Description,
            areaId.Value.Value,
            priority.Value,
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
        return await EnrichProjectAsync(ProjectResponse.FromProject(project, Today), includeTags: false, ct);
    }

    public async Task<ErrorOr<ChangeProjectStatusResponse>> ChangeProjectStatusAsync(ChangeProjectStatusCommand cmd, CancellationToken ct)
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

        var previousStatus = project.Status;
        var changed = project.ChangeStatus(status.Value, clock);
        if (changed.IsError)
        {
            return changed.Errors;
        }

        if (status.Value == ProjectStatus.Done)
        {
            var linkedIssues = await db.Issues
                .Where(issue => issue.HouseholdId == cmd.HouseholdId
                    && issue.LinkedProjectId == project.Id.Value
                    && issue.Status != PropertyIssueStatus.Closed)
                .ToListAsync(ct);

            foreach (var issue in linkedIssues)
            {
                var closed = issue.CloseFromProject(clock);
                if (closed.IsError)
                {
                    return closed.Errors;
                }
            }
        }

        var activityResult = activity.Append(
            cmd.HouseholdId,
            PropertyActivityVerb.ProjectStatusChanged,
            PropertyActivityTargetType.Project,
            project.Id.Value,
            $"Project \"{project.Name}\" moved from {previousStatus} to {project.Status}.",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["from"] = previousStatus.ToString(),
                ["to"] = project.Status.ToString()
            });
        if (activityResult.IsError)
        {
            return activityResult.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.status_changed", "Project", project.Id.Value, null, ct);

        SuggestedHistoryEntryResponse? suggested = null;
        if (status.Value == ProjectStatus.Done && project.CompletedAt is not null)
        {
            var budget = await GetProjectBudgetAsync(new GetProjectBudgetQuery(project.Id.Value, cmd.HouseholdId), ct);
            if (budget.IsError)
            {
                return budget.Errors;
            }

            var suggestedAreaName = await PropertyAreaTagEnrichment.AreaNameAsync(db, cmd.HouseholdId, project.AreaId?.Value, ct);
            suggested = new SuggestedHistoryEntryResponse(
                DateOnly.FromDateTime(project.CompletedAt.Value.UtcDateTime),
                project.Name,
                project.AreaId?.Value,
                suggestedAreaName,
                budget.Value.LinkedTotal,
                "Project",
                project.Id.Value,
                null,
                project.Attachments.Select(a => new SuggestedHistoryAttachmentResponse(a.BlobContainer, a.BlobKey)).ToArray());
        }

        var enriched = await EnrichProjectAsync(ProjectResponse.FromProject(project, Today), includeTags: false, ct);
        return new ChangeProjectStatusResponse(enriched, suggested);
    }

    public async Task<ErrorOr<Deleted>> DeleteProjectAsync(DeleteProjectCommand cmd, CancellationToken ct)
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
        QueueBlobDeletions(cmd.HouseholdId, attachments);
        await db.SaveChangesAsync(ct);

        await bus.PublishAsync(new ProjectDeletedV1(cmd.HouseholdId, cmd.ProjectId, Guid.NewGuid()));
        await audit.PublishAsync(cmd.HouseholdId, "property.project.deleted", "Project", cmd.ProjectId, null, ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<ProjectResponse>> GetProjectAsync(GetProjectQuery query, CancellationToken ct)
    {
        var project = await LoadProjectAsync(query.HouseholdId, query.ProjectId, ct, tracking: false);
        return project is null
            ? PropertyErrors.ProjectNotFound
            : await EnrichProjectAsync(ProjectResponse.FromProject(project, Today), includeTags: true, ct);
    }

    public async Task<ErrorOr<ListProjectsResponse>> ListProjectsAsync(ListProjectsQuery query, CancellationToken ct)
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

        if (query.AreaId is not null)
        {
            projects = projects.Where(project => project.AreaId == new PropertyAreaId(query.AreaId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            var priority = ParseProjectPriority(query.Priority);
            if (priority is null)
            {
                return PropertyErrors.ProjectPriorityInvalid;
            }

            projects = projects.Where(project => project.Priority == priority.Value);
        }

        if (query.TagIds is { Count: > 0 })
        {
            var tagIds = query.TagIds.Distinct().Select(id => new PropertyTagId(id)).ToArray();
            var matchingProjectIds = (await db.TagAssignments
                .AsNoTracking()
                .Where(assignment => assignment.HouseholdId == query.HouseholdId
                    && assignment.TargetType == PropertyTagTargetType.Project
                    && tagIds.Contains(assignment.TagId))
                .GroupBy(assignment => assignment.TargetId)
                .Where(group => group.Select(assignment => assignment.TagId).Distinct().Count() == tagIds.Length)
                .Select(group => group.Key)
                .ToArrayAsync(ct))
                .Select(id => new ProjectId(id))
                .ToArray();

            projects = projects.Where(project => matchingProjectIds.Contains(project.Id));
        }

        var today = Today;
        if (query.IsOverdue is not null)
        {
            projects = query.IsOverdue.Value
                ? projects.Where(project => project.Status != ProjectStatus.Done && project.TargetEndDate < today)
                : projects.Where(project => project.TargetEndDate == null || project.TargetEndDate >= today || project.Status == ProjectStatus.Done);
        }

        var totalCount = await projects.CountAsync(ct);
        var rows = await projects
            .OrderBy(project => project.Status)
            .ThenBy(project => project.Name)
            .Take(maxListItems + 1)
            .Select(project => new ProjectListItemResponse(
                project.Id.Value,
                project.HouseholdId,
                project.Name,
                project.Description,
                project.Status.ToString(),
                project.AreaId == null ? null : project.AreaId.Value.Value,
                null,
                project.Priority.ToString(),
                project.TargetStartDate,
                project.TargetEndDate,
                project.BudgetEstimate == null ? null : new MoneyDto(project.BudgetEstimate.Amount, project.BudgetEstimate.Currency),
                project.CompletedAt,
                project.Notes,
                project.Status != ProjectStatus.Done && project.TargetEndDate != null && project.TargetEndDate < today,
                project.Status != ProjectStatus.Done && project.TargetEndDate != null && project.TargetEndDate < today ? project.TargetEndDate : null,
                project.Status != ProjectStatus.Done && project.TargetEndDate != null && project.TargetEndDate < today ? today.DayNumber - project.TargetEndDate.Value.DayNumber : 0))
            .ToArrayAsync(ct);

        var pageRows = rows.Take(maxListItems).ToArray();
        var areaNames = await PropertyAreaTagEnrichment.AreaNameMapAsync(db, query.HouseholdId, ct);
        var tagsByProject = await PropertyAreaTagEnrichment.TagsByTargetAsync(
            db, query.HouseholdId, PropertyTagTargetType.Project, pageRows.Select(row => row.ProjectId).ToArray(), ct);

        var items = pageRows
            .Select(row => row with
            {
                AreaName = row.AreaId is null ? null : areaNames.GetValueOrDefault(row.AreaId.Value),
                Tags = tagsByProject.GetValueOrDefault(row.ProjectId, [])
            })
            .ToArray();

        return new ListProjectsResponse(items, rows.Length > maxListItems, totalCount);
    }

    public async Task<ErrorOr<ProjectTaskResponse>> AddTaskAsync(AddTaskCommand cmd, CancellationToken ct)
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
        return ProjectTaskResponse.FromTask(task.Value, Today);
    }

    public async Task<ErrorOr<ProjectTaskResponse>> UpdateTaskAsync(UpdateTaskCommand cmd, CancellationToken ct)
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
        return ProjectTaskResponse.FromTask(project.Tasks.Single(task => task.Id == taskId), Today);
    }

    public async Task<ErrorOr<Deleted>> DeleteTaskAsync(DeleteTaskCommand cmd, CancellationToken ct)
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

    public async Task<ErrorOr<GetProjectTasksResponse>> GetProjectTasksAsync(GetProjectTasksQuery query, CancellationToken ct)
    {
        var project = await LoadProjectAsync(query.HouseholdId, query.ProjectId, ct, tracking: false);
        if (project is null)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var today = Today;
        var tasks = project.Tasks.AsEnumerable();
        if (query.IsOverdue is not null)
        {
            tasks = query.IsOverdue.Value
                ? tasks.Where(task => task.Status != ProjectTaskStatus.Done && task.DueDate is not null && task.DueDate < today)
                : tasks.Where(task => task.DueDate is null || task.DueDate >= today || task.Status == ProjectTaskStatus.Done);
        }

        var orderedTasks = tasks.OrderBy(task => task.SortOrder).ToArray();
        return new GetProjectTasksResponse(
            orderedTasks.Take(maxListItems).Select(task => ProjectTaskResponse.FromTask(task, today)).ToArray(),
            orderedTasks.Length > maxListItems,
            orderedTasks.Length);
    }

    public async Task<ErrorOr<GetProjectTasksResponse>> ReorderTasksAsync(ReorderTasksCommand cmd, CancellationToken ct)
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
        return new GetProjectTasksResponse(project.Tasks.OrderBy(task => task.SortOrder).Select(task => ProjectTaskResponse.FromTask(task, Today)).ToArray());
    }

    public async Task<ErrorOr<ProjectLinkResponse>> AddLinkAsync(AddLinkCommand cmd, CancellationToken ct)
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

    public async Task<ErrorOr<Deleted>> RemoveLinkAsync(RemoveLinkCommand cmd, CancellationToken ct)
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

    public async Task<ErrorOr<ProjectAttachmentResponse>> AddAttachmentAsync(AddAttachmentCommand cmd, CancellationToken ct)
    {
        if (!ProjectAttachmentRules.HasAllowedSignature(cmd.ContentType, cmd.Content))
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

    public async Task<ErrorOr<AttachmentContentResponse>> GetAttachmentContentAsync(GetAttachmentContentQuery query, CancellationToken ct)
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

        try
        {
            var content = await blobStore.GetAsync(new BlobRef(attachment.BlobContainer, attachment.BlobKey), ct);
            return new AttachmentContentResponse(content.Stream, attachment.ContentType, attachment.FileName);
        }
        catch (System.IO.FileNotFoundException)
        {
            // DB/blob drift: the ownership row exists but the underlying blob is gone. Serve 404, not 500.
            return PropertyErrors.AttachmentNotFound;
        }
    }

    public async Task<ErrorOr<Deleted>> RemoveAttachmentAsync(RemoveAttachmentCommand cmd, CancellationToken ct)
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

        QueueBlobDeletions(cmd.HouseholdId, [new BlobRef(attachment.Value.BlobContainer, attachment.Value.BlobKey)]);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.attachment_removed", "ProjectAttachment", cmd.AttachmentId, null, ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<GetProjectBudgetResponse>> GetProjectBudgetAsync(GetProjectBudgetQuery query, CancellationToken ct)
    {
        var estimate = await db.Projects
            .AsNoTracking()
            .Where(project => project.HouseholdId == query.HouseholdId && project.Id == new ProjectId(query.ProjectId))
            .Select(project => project.BudgetEstimate)
            .ToListAsync(ct);
        if (estimate.Count == 0)
        {
            return PropertyErrors.ProjectNotFound;
        }

        var budgetEstimate = estimate[0];

        var summary = await bus.InvokeAsync<GetProjectSpendSummaryResult>(
            new GetProjectSpendSummaryQuery(query.HouseholdId, [query.ProjectId]),
            ct);
        var spend = summary.Summaries.SingleOrDefault(item => item.ProjectId == query.ProjectId);

        var linkedTotal = spend?.LinkedTotal ?? new MoneyDto(0m, defaultCurrency);
        var transactionCount = spend?.TransactionCount ?? 0;

        var estimateDto = budgetEstimate is null
            ? null
            : new MoneyDto(budgetEstimate.Amount, budgetEstimate.Currency);
        var remaining = estimateDto is null
            ? null
            : new MoneyDto(estimateDto.Amount - linkedTotal.Amount, estimateDto.Currency);

        return new GetProjectBudgetResponse(estimateDto, linkedTotal, remaining, transactionCount);
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

    private static ProjectPriority? ParseProjectPriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
        {
            return ProjectPriority.Medium;
        }

        return Enum.TryParse<ProjectPriority>(priority, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;
    }

    private static ProjectTaskStatus? ParseTaskStatus(string status) =>
        Enum.TryParse<ProjectTaskStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;

    private async Task<ProjectResponse> EnrichProjectAsync(ProjectResponse response, bool includeTags, CancellationToken ct)
    {
        var areaName = await PropertyAreaTagEnrichment.AreaNameAsync(db, response.HouseholdId, response.AreaId, ct);
        var tags = includeTags
            ? await PropertyAreaTagEnrichment.TagsForTargetAsync(db, response.HouseholdId, PropertyTagTargetType.Project, response.ProjectId, ct)
            : response.Tags;
        return response with { AreaName = areaName, Tags = tags };
    }

    private DateOnly Today => DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

    // Enqueue blob deletions in the same transaction as the owning-row removal so the
    // external delete is retried by the recurring drain job if storage is transiently
    // unavailable. Deleting the blob inline post-commit would orphan it on failure.
    private void QueueBlobDeletions(Guid householdId, IEnumerable<BlobRef> blobs)
    {
        foreach (var blob in blobs)
        {
            db.PendingBlobDeletions.Add(
                PropertyBlobDeletion.Create(householdId, blob.Container, blob.Key, clock.UtcNow));
        }
    }

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

    private async Task<ErrorOr<OptionalAreaId>> ValidateAreaAsync(Guid householdId, Guid? areaId, CancellationToken ct)
    {
        if (areaId is null)
        {
            return new OptionalAreaId(null);
        }

        var typedId = new PropertyAreaId(areaId.Value);
        var exists = await db.Areas.AnyAsync(area => area.HouseholdId == householdId && area.Id == typedId, ct);
        return exists ? new OptionalAreaId(typedId) : PropertyErrors.AreaNotFound;
    }

    private sealed record OptionalAreaId(PropertyAreaId? Value);
}
