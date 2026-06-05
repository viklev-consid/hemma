using Hemma.Modules.Economy.Features.Import.Contracts;

namespace Hemma.Modules.Economy.Features.Import.CommitImport;

public sealed record CommitImportCommand(Guid HouseholdId, Guid AccountId, string PreviewFingerprint, IReadOnlyList<NormalizedImportRowRequest> Rows);
