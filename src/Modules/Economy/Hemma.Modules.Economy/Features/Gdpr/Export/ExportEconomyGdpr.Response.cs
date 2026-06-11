namespace Hemma.Modules.Economy.Features.Gdpr.Export;

public sealed record ExportEconomyGdprResponse(
    Guid HouseholdId,
    DateTimeOffset ExportedAt,
    IReadOnlyDictionary<string, object?> Data);
