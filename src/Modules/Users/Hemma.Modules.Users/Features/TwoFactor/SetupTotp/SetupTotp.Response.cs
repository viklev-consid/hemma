namespace Hemma.Modules.Users.Features.TwoFactor.SetupTotp;

public sealed record SetupTotpResponse(string Secret, string OtpAuthUri);
