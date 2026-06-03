namespace Hemma.Modules.Users.Features.TwoFactor.DisableTwoFactor;

public sealed record DisableTwoFactorCommand(Guid UserId, string CurrentPassword, string Code, string? ActiveRefreshTokenId);
