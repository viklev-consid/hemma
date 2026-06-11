namespace Hemma.Modules.Households.Features.UpdateHousehold;

public sealed record UpdateHouseholdResponse(Guid HouseholdId, string Name, string Slug);
