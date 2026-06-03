namespace Hemma.Modules.Users.Features.TwoFactor.DisableTwoFactor;

public sealed record DisableTwoFactorRequest(string CurrentPassword, string Code);
