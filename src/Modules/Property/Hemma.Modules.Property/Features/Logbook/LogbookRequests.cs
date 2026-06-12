using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.Logbook;

public sealed record HistoryEntryRequest(
    Guid HouseholdId,
    DateOnly Date,
    string Title,
    Guid? AreaId,
    MoneyDto? Cost,
    string Type,
    Guid? SourceProjectId,
    Guid? SourceMaintenanceOccurrenceId,
    IReadOnlyList<HistoryPhotoRefRequest> PhotoRefs);

public sealed record HistoryPhotoRefRequest(string Container, string Key);
