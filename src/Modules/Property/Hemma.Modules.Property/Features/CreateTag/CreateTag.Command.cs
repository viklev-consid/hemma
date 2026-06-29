namespace Hemma.Modules.Property.Features.CreateTag;

public sealed record CreateTagCommand(Guid HouseholdId, string Name, string? Color);
