namespace Hemma.Modules.Households.Contracts.Events;

public sealed record HouseholdCreatedV1(
    Guid HouseholdId,
    string Name,
    string Slug,
    Guid CreatedByUserId,
    Guid EventId);
