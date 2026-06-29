using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Property.Domain;

public sealed class ProjectTask : Entity<ProjectTaskId>
{
    private ProjectTask(
        ProjectTaskId id,
        ProjectId projectId,
        string title,
        ProjectTaskStatus status,
        Money? estimate,
        Guid? assigneeId,
        DateOnly? dueDate,
        int sortOrder) : base(id)
    {
        ProjectId = projectId;
        Title = title;
        Status = status;
        Estimate = estimate;
        AssigneeId = assigneeId;
        DueDate = dueDate;
        SortOrder = sortOrder;
    }

    private ProjectTask() : base(default!) { }

    public ProjectId ProjectId { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public ProjectTaskStatus Status { get; private set; }
    public Money? Estimate { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public int SortOrder { get; private set; }

    public static ErrorOr<ProjectTask> Create(
        ProjectId projectId,
        string title,
        ProjectTaskStatus status,
        Money? estimate,
        Guid? assigneeId,
        DateOnly? dueDate,
        int sortOrder)
    {
        var normalizedTitle = NormalizeRequired(title, 200);
        if (normalizedTitle is null)
        {
            return PropertyErrors.TaskTitleInvalid;
        }

        if (!Enum.IsDefined(status))
        {
            return PropertyErrors.TaskStatusInvalid;
        }

        return new ProjectTask(ProjectTaskId.New(), projectId, normalizedTitle, status, estimate, assigneeId, dueDate, sortOrder);
    }

    public ErrorOr<Success> Update(string title, ProjectTaskStatus status, Money? estimate, Guid? assigneeId, DateOnly? dueDate)
    {
        var normalizedTitle = NormalizeRequired(title, 200);
        if (normalizedTitle is null)
        {
            return PropertyErrors.TaskTitleInvalid;
        }

        if (!Enum.IsDefined(status))
        {
            return PropertyErrors.TaskStatusInvalid;
        }

        Title = normalizedTitle;
        Status = status;
        Estimate = estimate;
        AssigneeId = assigneeId;
        DueDate = dueDate;
        return Result.Success;
    }

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    private static string? NormalizeRequired(string value, int maxLength)
    {
        var normalized = value.Trim();
        return normalized.Length is 0 || normalized.Length > maxLength ? null : normalized;
    }
}
