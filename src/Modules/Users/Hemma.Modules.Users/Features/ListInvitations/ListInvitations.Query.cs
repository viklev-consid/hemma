namespace Hemma.Modules.Users.Features.ListInvitations;

public sealed record ListInvitationsQuery(int Page = 1, int PageSize = 20, string? Status = "pending");
