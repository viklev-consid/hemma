using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Features.DeleteHousehold;

public sealed record DeleteHouseholdCommand(HouseholdId HouseholdId, Guid DeletedByUserId);
