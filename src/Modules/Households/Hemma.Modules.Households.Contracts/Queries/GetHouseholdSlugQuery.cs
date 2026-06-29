namespace Hemma.Modules.Households.Contracts.Queries;

/// <summary>
/// Cross-module query resolving a household's current URL-facing slug from its durable
/// <see cref="HouseholdId"/>. Other modules invoke this via <c>IMessageBus</c> when they need to
/// build a household-scoped deep link (e.g. notification hrefs under <c>/app/h/{slug}/...</c>).
/// Returns <c>null</c> when no active household matches the id.
/// </summary>
public sealed record GetHouseholdSlugQuery(Guid HouseholdId);

public sealed record GetHouseholdSlugResult(string Slug);
