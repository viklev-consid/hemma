namespace Hemma.Modules.Property.Features.AddLink;

public sealed record AddLinkCommand(Guid ProjectId, Guid HouseholdId, string Label, string Url);
