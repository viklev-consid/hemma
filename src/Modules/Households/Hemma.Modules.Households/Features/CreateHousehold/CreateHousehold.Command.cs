namespace Hemma.Modules.Households.Features.CreateHousehold;

public sealed record CreateHouseholdCommand(string Name, string? Slug, Guid CreatedByUserId);
