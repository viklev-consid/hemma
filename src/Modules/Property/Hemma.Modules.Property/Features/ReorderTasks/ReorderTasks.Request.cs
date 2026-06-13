namespace Hemma.Modules.Property.Features.ReorderTasks;

public sealed record ReorderTasksRequest(Guid HouseholdId, IReadOnlyList<Guid> TaskIds);
