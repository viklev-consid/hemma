namespace Hemma.Modules.Property.Features.GetProjectTasks;

public sealed record GetProjectTasksQuery(Guid ProjectId, Guid HouseholdId, bool? IsOverdue);
