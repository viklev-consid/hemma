using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Property.Domain;

public sealed class Project : AggregateRoot<ProjectId>
{
    private readonly List<ProjectTask> tasks = [];
    private readonly List<ProjectLink> links = [];
    private readonly List<ProjectAttachment> attachments = [];

    private Project(
        ProjectId id,
        Guid householdId,
        string name,
        string? description,
        ProjectStatus status,
        PropertyAreaId? areaId,
        ProjectPriority priority,
        DateOnly? targetStartDate,
        DateOnly? targetEndDate,
        Money? budgetEstimate,
        string? notes) : base(id)
    {
        HouseholdId = householdId;
        Name = name;
        Description = description;
        Status = status;
        AreaId = areaId;
        Priority = priority;
        TargetStartDate = targetStartDate;
        TargetEndDate = targetEndDate;
        BudgetEstimate = budgetEstimate;
        Notes = notes;
    }

    private Project() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; }
    public PropertyAreaId? AreaId { get; private set; }
    public ProjectPriority Priority { get; private set; }
    public DateOnly? TargetStartDate { get; private set; }
    public DateOnly? TargetEndDate { get; private set; }
    public Money? BudgetEstimate { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Notes { get; private set; }
    public IReadOnlyCollection<ProjectTask> Tasks => tasks;
    public IReadOnlyCollection<ProjectLink> Links => links;
    public IReadOnlyCollection<ProjectAttachment> Attachments => attachments;

    public static ErrorOr<Project> Create(
        Guid householdId,
        string name,
        string? description,
        ProjectStatus status,
        PropertyAreaId? areaId,
        ProjectPriority priority,
        DateOnly? targetStartDate,
        DateOnly? targetEndDate,
        Money? budgetEstimate,
        string? notes)
    {
        var details = ValidateDetails(name, description, targetStartDate, targetEndDate, notes);
        if (details.IsError)
        {
            return details.Errors;
        }

        if (!Enum.IsDefined(status))
        {
            return PropertyErrors.ProjectStatusInvalid;
        }

        if (!Enum.IsDefined(priority))
        {
            return PropertyErrors.ProjectPriorityInvalid;
        }

        var project = new Project(
            ProjectId.New(),
            householdId,
            details.Value.Name,
            details.Value.Description,
            status,
            areaId,
            priority,
            targetStartDate,
            targetEndDate,
            budgetEstimate,
            details.Value.Notes);

        return project;
    }

    public ErrorOr<Success> UpdateDetails(
        string name,
        string? description,
        PropertyAreaId? areaId,
        ProjectPriority priority,
        DateOnly? targetStartDate,
        DateOnly? targetEndDate,
        Money? budgetEstimate,
        string? notes)
    {
        var details = ValidateDetails(name, description, targetStartDate, targetEndDate, notes);
        if (details.IsError)
        {
            return details.Errors;
        }

        if (!Enum.IsDefined(priority))
        {
            return PropertyErrors.ProjectPriorityInvalid;
        }

        Name = details.Value.Name;
        Description = details.Value.Description;
        AreaId = areaId;
        Priority = priority;
        TargetStartDate = targetStartDate;
        TargetEndDate = targetEndDate;
        BudgetEstimate = budgetEstimate;
        Notes = details.Value.Notes;
        return Result.Success;
    }

    public ErrorOr<Success> ChangeStatus(ProjectStatus status, IClock clock)
    {
        if (!Enum.IsDefined(status))
        {
            return PropertyErrors.ProjectStatusInvalid;
        }

        Status = status;
        CompletedAt = status == ProjectStatus.Done ? clock.UtcNow : null;
        return Result.Success;
    }

    public ErrorOr<ProjectTask> AddTask(
        string title,
        ProjectTaskStatus status,
        Money? estimate,
        Guid? assigneeId,
        DateOnly? dueDate)
    {
        var task = ProjectTask.Create(Id, title, status, estimate, assigneeId, dueDate, tasks.Count);
        if (task.IsError)
        {
            return task.Errors;
        }

        tasks.Add(task.Value);
        return task.Value;
    }

    public ErrorOr<Success> UpdateTask(ProjectTaskId taskId, string title, ProjectTaskStatus status, Money? estimate, Guid? assigneeId, DateOnly? dueDate)
    {
        var task = tasks.FirstOrDefault(x => x.Id == taskId);
        if (task is null)
        {
            return PropertyErrors.TaskNotFound;
        }

        return task.Update(title, status, estimate, assigneeId, dueDate);
    }

    public ErrorOr<Success> RemoveTask(ProjectTaskId taskId)
    {
        var task = tasks.FirstOrDefault(x => x.Id == taskId);
        if (task is null)
        {
            return PropertyErrors.TaskNotFound;
        }

        tasks.Remove(task);
        ReassignTaskOrder();
        return Result.Success;
    }

    public ErrorOr<Success> ReorderTasks(IReadOnlyCollection<ProjectTaskId> orderedTaskIds)
    {
        if (orderedTaskIds.Count != tasks.Count || orderedTaskIds.Distinct().Count() != tasks.Count)
        {
            return PropertyErrors.TaskOrderInvalid;
        }

        var byId = tasks.ToDictionary(task => task.Id);
        if (orderedTaskIds.Any(id => !byId.ContainsKey(id)))
        {
            return PropertyErrors.TaskOrderInvalid;
        }

        var sortOrder = 0;
        foreach (var taskId in orderedTaskIds)
        {
            byId[taskId].SetSortOrder(sortOrder++);
        }

        tasks.Sort((left, right) => left.SortOrder.CompareTo(right.SortOrder));
        return Result.Success;
    }

    public ErrorOr<ProjectLink> AddLink(string label, string url)
    {
        var link = ProjectLink.Create(Id, label, url);
        if (link.IsError)
        {
            return link.Errors;
        }

        links.Add(link.Value);
        return link.Value;
    }

    public ErrorOr<Success> RemoveLink(ProjectLinkId linkId)
    {
        var link = links.FirstOrDefault(x => x.Id == linkId);
        if (link is null)
        {
            return PropertyErrors.LinkNotFound;
        }

        links.Remove(link);
        return Result.Success;
    }

    public ErrorOr<ProjectAttachment> AddAttachment(
        string blobContainer,
        string blobKey,
        string fileName,
        string contentType,
        long size)
    {
        var attachment = ProjectAttachment.Create(Id, blobContainer, blobKey, fileName, contentType, size);
        if (attachment.IsError)
        {
            return attachment.Errors;
        }

        attachments.Add(attachment.Value);
        return attachment.Value;
    }

    public ErrorOr<ProjectAttachment> RemoveAttachment(ProjectAttachmentId attachmentId)
    {
        var attachment = attachments.FirstOrDefault(x => x.Id == attachmentId);
        if (attachment is null)
        {
            return PropertyErrors.AttachmentNotFound;
        }

        attachments.Remove(attachment);
        return attachment;
    }

    private void ReassignTaskOrder()
    {
        var sortOrder = 0;
        foreach (var task in tasks.OrderBy(task => task.SortOrder))
        {
            task.SetSortOrder(sortOrder++);
        }
    }

    private static ErrorOr<ProjectDetails> ValidateDetails(
        string name,
        string? description,
        DateOnly? targetStartDate,
        DateOnly? targetEndDate,
        string? notes)
    {
        var normalizedName = NormalizeRequired(name, 160);
        if (normalizedName is null)
        {
            return PropertyErrors.ProjectNameInvalid;
        }

        var normalizedDescription = NormalizeOptional(description, 2000);
        if (normalizedDescription.IsError)
        {
            return PropertyErrors.ProjectDescriptionInvalid;
        }

        var normalizedNotes = NormalizeOptional(notes, 4000);
        if (normalizedNotes.IsError)
        {
            return PropertyErrors.ProjectNotesInvalid;
        }

        if (targetStartDate is not null && targetEndDate is not null && targetEndDate < targetStartDate)
        {
            return PropertyErrors.ProjectDateRangeInvalid;
        }

        return new ProjectDetails(
            normalizedName,
            normalizedDescription.Value.Value,
            normalizedNotes.Value.Value);
    }

    private static string? NormalizeRequired(string value, int maxLength)
    {
        var normalized = value.Trim();
        return normalized.Length is 0 || normalized.Length > maxLength ? null : normalized;
    }

    private static ErrorOr<OptionalString> NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new OptionalString(null);
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength ? PropertyErrors.ProjectDescriptionInvalid : new OptionalString(normalized);
    }

    private sealed record OptionalString(string? Value);

    private sealed record ProjectDetails(string Name, string? Description, string? Notes);
}
