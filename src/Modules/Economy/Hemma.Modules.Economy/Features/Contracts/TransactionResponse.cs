using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.Features.Contracts;

public sealed record TransactionResponse(
    Guid TransactionId,
    Guid HouseholdId,
    Guid AccountId,
    Guid? CategoryId,
    MoneyResponse Amount,
    DateOnly OccurredOn,
    string? Note,
    string Kind,
    bool IsPending,
    bool HasReceipt,
    Guid? PayerId)
{
    public static TransactionResponse From(Transaction transaction) =>
        new(
            transaction.Id.Value,
            transaction.HouseholdId,
            transaction.AccountId.Value,
            transaction.CategoryId?.Value,
            MoneyResponse.From(transaction.Amount),
            transaction.OccurredOn,
            transaction.Note,
            transaction.Kind.Name,
            transaction.IsPending,
            transaction.HasReceipt,
            transaction.PayerId);
}
