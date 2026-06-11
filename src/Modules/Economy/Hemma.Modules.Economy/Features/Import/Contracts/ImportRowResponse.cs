using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.Import.Contracts;

public sealed record ImportRowResponse(
    int RowNumber,
    DateOnly? OccurredOn,
    MoneyDto? Amount,
    string? Description,
    string? Currency,
    string? Counterparty,
    string? Reference,
    MoneyDto? BalanceAfter,
    string? RawDescription,
    Guid? SuggestedCategoryId,
    Guid? SelectedCategoryId,
    string DuplicateState,
    string RowFingerprint,
    IReadOnlyList<SubscriptionMatchSuggestionResponse> SuggestedSubscriptionMatches,
    IReadOnlyList<ImportRowValidationErrorResponse> Errors);
