namespace Hemma.Modules.Property.Features.CreateArea;

public sealed record CreateAreaCommand(Guid HouseholdId, string Name, string? Description);
