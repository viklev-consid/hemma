namespace Hemma.Modules.Households.Contracts.Queries;

/// <summary>
/// Cross-module query returning the active members of a household. Other modules invoke this
/// via <c>IMessageBus</c> when they need to fan out to every member (e.g. notifications).
/// </summary>
public sealed record ListHouseholdMembersQuery(Guid HouseholdId);

public sealed record ListHouseholdMembersResult(IReadOnlyList<HouseholdMemberInfo> Members);

public sealed record HouseholdMemberInfo(Guid UserId, string Role);
