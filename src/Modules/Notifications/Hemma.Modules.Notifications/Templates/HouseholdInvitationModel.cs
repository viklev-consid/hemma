namespace Hemma.Modules.Notifications.Templates;

public sealed record HouseholdInvitationModel(
    string Role,
    string Token,
    string InvitationUrl);
