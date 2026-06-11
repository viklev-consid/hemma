using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Property.Domain;

public sealed class HistoryEntry : AggregateRoot<HistoryEntryId>
{
    private readonly List<HistoryEntryPhoto> photos = [];

    private HistoryEntry(
        HistoryEntryId id,
        Guid householdId,
        DateOnly date,
        string title,
        string? area,
        Money? cost,
        HistoryEntryType type,
        Guid? sourceProjectId,
        Guid? sourceMaintenanceOccurrenceId) : base(id)
    {
        HouseholdId = householdId;
        Date = date;
        Title = title;
        Area = area;
        Cost = cost;
        Type = type;
        SourceProjectId = sourceProjectId;
        SourceMaintenanceOccurrenceId = sourceMaintenanceOccurrenceId;
    }

    private HistoryEntry() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Area { get; private set; }
    public Money? Cost { get; private set; }
    public HistoryEntryType Type { get; private set; }
    public Guid? SourceProjectId { get; private set; }
    public Guid? SourceMaintenanceOccurrenceId { get; private set; }
    public IReadOnlyCollection<HistoryEntryPhoto> Photos => photos;

    public static ErrorOr<HistoryEntry> Create(
        Guid householdId,
        DateOnly date,
        string title,
        string? area,
        Money? cost,
        HistoryEntryType type,
        Guid? sourceProjectId,
        Guid? sourceMaintenanceOccurrenceId)
    {
        var details = ValidateDetails(title, area, type, sourceProjectId, sourceMaintenanceOccurrenceId);
        if (details.IsError)
        {
            return details.Errors;
        }

        return new HistoryEntry(
            HistoryEntryId.New(),
            householdId,
            date,
            details.Value.Title,
            details.Value.Area,
            cost,
            type,
            sourceProjectId,
            sourceMaintenanceOccurrenceId);
    }

    public ErrorOr<Success> Update(
        DateOnly date,
        string title,
        string? area,
        Money? cost,
        HistoryEntryType type,
        Guid? sourceProjectId,
        Guid? sourceMaintenanceOccurrenceId)
    {
        var details = ValidateDetails(title, area, type, sourceProjectId, sourceMaintenanceOccurrenceId);
        if (details.IsError)
        {
            return details.Errors;
        }

        Date = date;
        Title = details.Value.Title;
        Area = details.Value.Area;
        Cost = cost;
        Type = type;
        SourceProjectId = sourceProjectId;
        SourceMaintenanceOccurrenceId = sourceMaintenanceOccurrenceId;
        return Result.Success;
    }

    public ErrorOr<HistoryEntryPhoto> AddPhoto(
        string blobContainer,
        string blobKey,
        string fileName,
        string contentType,
        long size)
    {
        var photo = HistoryEntryPhoto.Create(Id, blobContainer, blobKey, fileName, contentType, size);
        if (photo.IsError)
        {
            return photo.Errors;
        }

        photos.Add(photo.Value);
        return photo.Value;
    }

    private static ErrorOr<HistoryEntryDetails> ValidateDetails(
        string title,
        string? area,
        HistoryEntryType type,
        Guid? sourceProjectId,
        Guid? sourceMaintenanceOccurrenceId)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 160)
        {
            return PropertyErrors.HistoryEntryTitleInvalid;
        }

        var normalizedArea = string.IsNullOrWhiteSpace(area) ? null : area.Trim();
        if (normalizedArea is { Length: > 100 })
        {
            return PropertyErrors.HistoryEntryAreaInvalid;
        }

        if (!Enum.IsDefined(type))
        {
            return PropertyErrors.HistoryEntryTypeInvalid;
        }

        if (type == HistoryEntryType.Manual && (sourceProjectId is not null || sourceMaintenanceOccurrenceId is not null))
        {
            return PropertyErrors.HistoryEntrySourceInvalid;
        }

        if (type == HistoryEntryType.Project && sourceMaintenanceOccurrenceId is not null)
        {
            return PropertyErrors.HistoryEntrySourceInvalid;
        }

        if (type == HistoryEntryType.Maintenance && sourceProjectId is not null)
        {
            return PropertyErrors.HistoryEntrySourceInvalid;
        }

        return new HistoryEntryDetails(title.Trim(), normalizedArea);
    }

    private sealed record HistoryEntryDetails(string Title, string? Area);
}
