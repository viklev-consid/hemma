namespace Hemma.Modules.Households.Features.ListHouseholdMembers;

public sealed record ListHouseholdMembersResponse(
    IReadOnlyCollection<HouseholdMemberItem> Members,
    int Page,
    int PageSize,
    int Total);

public sealed record HouseholdMemberItem(
    Guid? UserId,
    string Role,
    DateTimeOffset JoinedAt,
    bool IsAnonymized,
    string? DisplayName,
    string? Email);
