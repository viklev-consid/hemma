namespace Hemma.Modules.Property.Features.CreateTag;

public sealed record PropertyTagRequest(Guid HouseholdId, string Name, string? Color);
