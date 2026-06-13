namespace Hemma.Modules.Property.Features.UpdateArea;

public sealed record PropertyAreaRequest(Guid HouseholdId, string Name, string? Description);
