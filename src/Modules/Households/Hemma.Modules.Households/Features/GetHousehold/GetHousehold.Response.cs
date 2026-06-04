namespace Hemma.Modules.Households.Features.GetHousehold;

public sealed record GetHouseholdResponse(Guid HouseholdId, string Name, string Slug, string AccessMode);
