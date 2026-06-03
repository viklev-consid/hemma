namespace Hemma.Modules.Notifications.Templates;

public sealed record EmailChangeRequestModel(
    string Token,
    string ConfirmationUrl);
