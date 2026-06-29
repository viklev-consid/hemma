namespace Hemma.Modules.Property.Features.ChangeProjectStatus;

public sealed record ChangeProjectStatusCommand(Guid ProjectId, Guid HouseholdId, string Status);
