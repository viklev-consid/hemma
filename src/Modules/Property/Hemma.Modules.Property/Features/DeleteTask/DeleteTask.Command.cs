namespace Hemma.Modules.Property.Features.DeleteTask;

public sealed record DeleteTaskCommand(Guid ProjectId, Guid TaskId, Guid HouseholdId);
