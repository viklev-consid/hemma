namespace Hemma.Modules.Households.Features.ListHouseholdInvitations;

public sealed record ListHouseholdInvitationsResponse(
    IReadOnlyCollection<HouseholdInvitationItem> Invitations,
    int Page,
    int PageSize,
    int Total);

public sealed record HouseholdInvitationItem(Guid InvitationId, string Email, string Role, DateTimeOffset ExpiresAt, bool IsPending);
