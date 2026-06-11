using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.Logbook;

public sealed record CreateHistoryEntryCommand(
    Guid HouseholdId,
    DateOnly Date,
    string Title,
    string? Area,
    MoneyDto? Cost,
    string Type,
    Guid? SourceProjectId,
    Guid? SourceMaintenanceOccurrenceId,
    IReadOnlyList<HistoryPhotoRefRequest> PhotoRefs);

public sealed record UpdateHistoryEntryCommand(
    Guid HistoryEntryId,
    Guid HouseholdId,
    DateOnly Date,
    string Title,
    string? Area,
    MoneyDto? Cost,
    string Type,
    Guid? SourceProjectId,
    Guid? SourceMaintenanceOccurrenceId);

public sealed record DeleteHistoryEntryCommand(Guid HistoryEntryId, Guid HouseholdId);

public sealed record ListHistoryQuery(Guid HouseholdId, int? Year, string? Area, string? Type);

public sealed record GetHistoryPhotoQuery(Guid HistoryEntryId, string BlobKey, Guid HouseholdId);
