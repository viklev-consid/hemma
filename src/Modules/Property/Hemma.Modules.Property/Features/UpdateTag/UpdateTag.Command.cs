namespace Hemma.Modules.Property.Features.UpdateTag;

public sealed record UpdateTagCommand(Guid TagId, Guid HouseholdId, string Name, string? Color);
