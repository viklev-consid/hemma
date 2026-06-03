namespace Hemma.Modules.Users.Features.ConfirmEmailChange;

public sealed record ConfirmEmailChangeCommand(Guid UserId, string Token);
