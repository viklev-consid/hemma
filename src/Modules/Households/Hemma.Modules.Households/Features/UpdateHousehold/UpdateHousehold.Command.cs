using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Features.UpdateHousehold;

public sealed record UpdateHouseholdCommand(HouseholdId HouseholdId, string Name, string Slug);
