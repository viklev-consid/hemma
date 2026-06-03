namespace Hemma.Modules.Users.Features.TwoFactor.RegenerateRecoveryCodes;

public sealed record RegenerateRecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);
