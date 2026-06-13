namespace Hemma.Modules.Property.Features.DeleteProject;

public sealed record DeleteProjectCommand(Guid ProjectId, Guid HouseholdId);
