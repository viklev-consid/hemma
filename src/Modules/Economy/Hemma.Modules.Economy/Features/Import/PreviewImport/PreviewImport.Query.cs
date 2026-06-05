using Hemma.Modules.Economy.Features.Import.Contracts;

namespace Hemma.Modules.Economy.Features.Import.PreviewImport;

public sealed record PreviewImportQuery(Guid HouseholdId, Guid AccountId, IReadOnlyList<NormalizedImportRowRequest> Rows);
