using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.CreateTransfer;

public sealed record CreateTransferRequest(
    Guid HouseholdId,
    Guid FromAccountId,
    Guid ToAccountId,
    MoneyRequest Amount,
    DateOnly OccurredOn,
    string? Note,
    string Mode,
    Guid? CategoryId,
    Guid? PayerId);
