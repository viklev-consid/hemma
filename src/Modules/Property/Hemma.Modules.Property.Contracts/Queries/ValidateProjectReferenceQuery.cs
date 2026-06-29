namespace Hemma.Modules.Property.Contracts.Queries;

public sealed record ValidateProjectReferenceQuery(Guid HouseholdId, Guid ProjectId);

public sealed record ValidateProjectReferenceResult(bool Exists);
