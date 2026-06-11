namespace Hemma.Modules.Households.Features.ListMyHouseholds;

public sealed record ListMyHouseholdsResponse(IReadOnlyCollection<MyHouseholdItem> Households);

public sealed record MyHouseholdItem(
    Guid HouseholdId,
    string Name,
    string Slug,
    string Role,
    IReadOnlyCollection<string> Permissions,
    string PermissionsVersion);
