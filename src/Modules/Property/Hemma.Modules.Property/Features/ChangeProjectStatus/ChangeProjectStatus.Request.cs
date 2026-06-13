namespace Hemma.Modules.Property.Features.ChangeProjectStatus;

public sealed record ChangeProjectStatusRequest(Guid HouseholdId, string Status);
