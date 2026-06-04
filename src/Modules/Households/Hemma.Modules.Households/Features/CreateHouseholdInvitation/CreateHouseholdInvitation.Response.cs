using Hemma.Shared.Kernel.Gdpr;

namespace Hemma.Modules.Households.Features.CreateHouseholdInvitation;

public sealed record CreateHouseholdInvitationResponse(
    Guid InvitationId,
    [property: PersonalData] string Email,
    string Role,
    DateTimeOffset ExpiresAt,
    [property: SensitivePersonalData] string RawToken);
