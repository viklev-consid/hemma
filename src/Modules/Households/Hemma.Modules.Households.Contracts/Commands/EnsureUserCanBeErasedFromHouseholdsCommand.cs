namespace Hemma.Modules.Households.Contracts.Commands;

public sealed record EnsureUserCanBeErasedFromHouseholdsCommand(Guid UserId);

public sealed record EnsureUserCanBeErasedFromHouseholdsResponse(
    IReadOnlyCollection<UserErasureBlockingHousehold> BlockingHouseholds)
{
    public bool CanBeErased => BlockingHouseholds.Count == 0;
}

public sealed record UserErasureBlockingHousehold(
    Guid HouseholdId,
    string Name,
    string Slug,
    string Role,
    bool IsSoleOwner);
