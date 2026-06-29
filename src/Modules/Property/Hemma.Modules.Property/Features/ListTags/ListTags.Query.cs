namespace Hemma.Modules.Property.Features.ListTags;

public sealed record ListTagsQuery(Guid HouseholdId, bool IncludeArchived);
