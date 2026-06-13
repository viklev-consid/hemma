namespace Hemma.Modules.Property.Features.DeleteHistoryEntry;

public sealed record DeleteHistoryEntryCommand(Guid HistoryEntryId, Guid HouseholdId);
