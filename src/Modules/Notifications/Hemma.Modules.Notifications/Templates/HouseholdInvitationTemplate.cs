namespace Hemma.Modules.Notifications.Templates;

internal static class HouseholdInvitationTemplate
{
    public const string Subject = "You're invited to an household";

    public static string PlainTextBody(string role, string token, string invitationUrl) =>
        $"You've been invited to join an household as {role}. Accept the invitation here: {invitationUrl}\n\nIf the link does not work, copy this token into the invitation screen: {token}\n\nIf you did not expect this invitation, you can ignore this email.";
}
