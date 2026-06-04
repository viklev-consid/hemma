using Hemma.Shared.Kernel.Gdpr;

namespace Hemma.Modules.Households.Contracts.Events;

public sealed record HouseholdInvitationCreatedV1(
    Guid HouseholdId,
    Guid InvitationId,
    [property: PersonalData] string Email,
    string Role,
    [property: SensitivePersonalData] string RawToken,
    Guid InvitedByUserId,
    Guid EventId);
