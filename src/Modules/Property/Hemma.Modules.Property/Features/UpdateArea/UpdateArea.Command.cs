namespace Hemma.Modules.Property.Features.UpdateArea;

public sealed record UpdateAreaCommand(Guid AreaId, Guid HouseholdId, string Name, string? Description);
