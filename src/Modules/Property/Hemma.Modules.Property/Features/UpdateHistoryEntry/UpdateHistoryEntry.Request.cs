using Hemma.Modules.Property.Features.Shared;
using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.UpdateHistoryEntry;

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
