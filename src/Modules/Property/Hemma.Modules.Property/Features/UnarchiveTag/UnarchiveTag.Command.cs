namespace Hemma.Modules.Property.Features.UnarchiveTag;

public sealed record UnarchiveTagCommand(Guid TagId, Guid HouseholdId);
