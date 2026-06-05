namespace Hemma.Modules.Economy.Features.CreateTransfer;

public sealed record CreateTransferCommand(
    Guid HouseholdId,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string Currency,
    DateOnly OccurredOn,
    string? Note,
    string Mode,
    Guid? CategoryId,
    Guid? PayerId);
