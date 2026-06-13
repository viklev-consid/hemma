namespace Hemma.Modules.Property.Features.RemoveLink;

public sealed record RemoveLinkCommand(Guid ProjectId, Guid LinkId, Guid HouseholdId);
