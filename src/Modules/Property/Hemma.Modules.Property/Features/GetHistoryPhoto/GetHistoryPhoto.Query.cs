namespace Hemma.Modules.Property.Features.GetHistoryPhoto;

public sealed record GetHistoryPhotoQuery(Guid HistoryEntryId, string BlobKey, Guid HouseholdId);
