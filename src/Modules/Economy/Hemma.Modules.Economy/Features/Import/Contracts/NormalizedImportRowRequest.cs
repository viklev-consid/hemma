using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.Import.Contracts;

public sealed record NormalizedImportRowRequest(
    int RowNumber,
    DateOnly? OccurredOn,
    decimal? Amount,
    string? Description,
    string? Currency,
    string? Counterparty,
    string? Reference,
    MoneyDto? BalanceAfter,
    string? RawDescription,
    Guid? CategoryId);
