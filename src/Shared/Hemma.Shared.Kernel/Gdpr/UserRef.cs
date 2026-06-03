namespace Hemma.Shared.Kernel.Gdpr;

public sealed record UserRef(Guid UserId, string? DisplayName = null);
