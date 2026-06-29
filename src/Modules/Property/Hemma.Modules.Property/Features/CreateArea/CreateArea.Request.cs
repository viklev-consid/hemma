namespace Hemma.Modules.Property.Features.CreateArea;

public sealed record PropertyAreaRequest(Guid HouseholdId, string Name, string? Description);
