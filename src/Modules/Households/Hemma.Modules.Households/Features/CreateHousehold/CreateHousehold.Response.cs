namespace Hemma.Modules.Households.Features.CreateHousehold;

public sealed record CreateHouseholdResponse(Guid HouseholdId, string Name, string Slug, string Role);
