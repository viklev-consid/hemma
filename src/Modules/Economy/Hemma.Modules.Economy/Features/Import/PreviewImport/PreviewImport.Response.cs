using Hemma.Modules.Economy.Features.Import.Contracts;

namespace Hemma.Modules.Economy.Features.Import.PreviewImport;

public sealed record PreviewImportResponse(Guid HouseholdId, Guid AccountId, string PreviewFingerprint, IReadOnlyList<ImportRowResponse> Rows);
