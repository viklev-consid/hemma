using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.Import.Contracts;

public sealed record ImportRowResponse(
    int RowNumber,
    DateOnly? OccurredOn,
    MoneyResponse? Amount,
    string? Description,
    string? Currency,
    string? Counterparty,
    string? Reference,
    MoneyResponse? BalanceAfter,
    string? RawDescription,
    Guid? SuggestedCategoryId,
    Guid? SelectedCategoryId,
    string DuplicateState,
    string RowFingerprint,
    IReadOnlyList<SubscriptionMatchSuggestionResponse> SuggestedSubscriptionMatches,
    IReadOnlyList<ImportRowValidationErrorResponse> Errors);
