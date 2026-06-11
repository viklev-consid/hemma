using Hemma.Modules.Households.Domain;
using Hemma.Shared.Infrastructure.Authorization;

namespace Hemma.Modules.Households.Features.GetHousehold;

public sealed record GetHouseholdQuery(HouseholdId HouseholdId, ScopedAuthorizationAccessMode AccessMode);
