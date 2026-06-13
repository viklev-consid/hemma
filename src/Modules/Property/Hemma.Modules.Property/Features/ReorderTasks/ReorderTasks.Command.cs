namespace Hemma.Modules.Property.Features.ReorderTasks;

public sealed record ReorderTasksCommand(Guid ProjectId, Guid HouseholdId, IReadOnlyList<Guid> TaskIds);
