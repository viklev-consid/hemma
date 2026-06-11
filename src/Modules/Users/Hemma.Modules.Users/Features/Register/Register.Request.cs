namespace Hemma.Modules.Users.Features.Register;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    string? InvitationToken = null,
    string? HouseholdInvitationToken = null);
