namespace Hemma.Modules.Economy.Features.SearchTransactionNote;

public sealed record SearchTransactionNoteQuery(Guid HouseholdId, string Search, int Page, int PageSize);
