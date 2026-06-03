using Hemma.Shared.Kernel.Gdpr;

namespace Hemma.Modules.Organizations.Features.CreateOrganizationInvitation;

public sealed record CreateOrganizationInvitationResponse(
    Guid InvitationId,
    [property: PersonalData] string Email,
    string Role,
    DateTimeOffset ExpiresAt,
    [property: SensitivePersonalData] string RawToken);
