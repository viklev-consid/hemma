namespace Hemma.Modules.Property.Features.AddLink;

public sealed record ProjectLinkRequest(Guid HouseholdId, string Label, string Url);
