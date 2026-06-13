namespace Hemma.Modules.Property.Features.UpdateTag;

public sealed record PropertyTagRequest(Guid HouseholdId, string Name, string? Color);
