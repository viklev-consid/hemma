using Hemma.Modules.Property.Domain;
using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.Logbook;

public sealed record HistoryEntryResponse(
    Guid HistoryEntryId,
    Guid HouseholdId,
    DateOnly Date,
    string Title,
    string? Area,
    MoneyDto? Cost,
    string Type,
    Guid? SourceProjectId,
    Guid? SourceMaintenanceOccurrenceId,
    IReadOnlyList<HistoryPhotoResponse> Photos)
{
    public static HistoryEntryResponse FromEntry(HistoryEntry entry) =>
        new(
            entry.Id.Value,
            entry.HouseholdId,
            entry.Date,
            entry.Title,
            entry.Area,
            entry.Cost is null ? null : new MoneyDto(entry.Cost.Amount, entry.Cost.Currency),
            entry.Type.ToString(),
            entry.SourceProjectId,
            entry.SourceMaintenanceOccurrenceId,
            entry.Photos.Select(HistoryPhotoResponse.FromPhoto).ToArray());
}

public sealed record HistoryPhotoResponse(string Container, string Key, string FileName, string ContentType, long Size)
{
    public static HistoryPhotoResponse FromPhoto(HistoryEntryPhoto photo) =>
        new(photo.BlobContainer, photo.BlobKey, photo.FileName, photo.ContentType, photo.Size);
}

public sealed record ListHistoryResponse(IReadOnlyList<HistoryEntryResponse> Entries);

public sealed record HistoryPhotoContentResponse(Stream Content, string ContentType, string FileName);
