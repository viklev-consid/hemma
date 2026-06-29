namespace Hemma.Modules.Property.Features.AssignTags;

public sealed record AssignTagsCommand(Guid HouseholdId, string TargetType, Guid TargetId, IReadOnlyList<Guid> TagIds);
