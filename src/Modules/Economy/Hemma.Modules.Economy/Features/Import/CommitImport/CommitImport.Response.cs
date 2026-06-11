using Hemma.Shared.Contracts;
using Hemma.Modules.Economy.Features.Import.Contracts;

namespace Hemma.Modules.Economy.Features.Import.CommitImport;

public sealed record CommitImportResponse(
    int ImportedCount,
    int DuplicateCount,
    IReadOnlyList<TransactionResponse> Transactions,
    IReadOnlyList<ImportRuleSuggestionResponse> SuggestedRules);
