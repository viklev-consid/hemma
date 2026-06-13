using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.UpdateHistoryEntry;

public sealed record UpdateHistoryEntryCommand(
    Guid HistoryEntryId,
    Guid HouseholdId,
    DateOnly Date,
    string Title,
    Guid? AreaId,
    MoneyDto? Cost,
    string Type,
    Guid? SourceProjectId,
    Guid? SourceMaintenanceOccurrenceId);
