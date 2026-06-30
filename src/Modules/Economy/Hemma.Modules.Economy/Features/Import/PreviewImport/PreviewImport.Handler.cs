using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Features.Import.Contracts;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.Import.PreviewImport;

public sealed class PreviewImportHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<PreviewImportResponse>> Handle(PreviewImportQuery query, CancellationToken ct)
    {
        var accountId = new AccountId(query.AccountId);
        if (!await db.Accounts.AnyAsync(x => x.HouseholdId == query.HouseholdId && x.Id == accountId, ct))
        {
            return EconomyErrors.AccountNotFound;
        }

        var existingFingerprints = await db.Transactions
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId && x.AccountId == accountId && x.ImportFingerprint != null)
            .Select(x => x.ImportFingerprint!)
            .ToListAsync(ct);
        var existingFingerprintSet = existingFingerprints.ToHashSet(StringComparer.Ordinal);

        var possibleDuplicates = await db.Transactions
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId && x.AccountId == accountId)
            .Select(x => new PossibleDuplicate(x.OccurredOn, x.Amount.Amount))
            .ToListAsync(ct);

        var rules = await db.CategorizationRules
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId && x.Enabled)
            .OrderBy(x => x.Pattern)
            .ToListAsync(ct);

        var subscriptions = await db.Subscriptions
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId && x.LifecycleState != SubscriptionLifecycleState.Cancelled)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        var pendingRecurringBills = await db.RecurringBills
            .AsNoTracking()
            .Include(x => x.Occurrences.Where(occurrence => occurrence.State == RecurringBillOccurrenceState.Pending))
            .Where(x => x.HouseholdId == query.HouseholdId && x.AccountId == accountId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        var pendingOccurrences = pendingRecurringBills
            .SelectMany(bill => bill.Occurrences.Select(occurrence => new PendingRecurringBillOccurrence(bill, occurrence)))
            .ToList();

        var rows = query.Rows
            .Select(row => EnrichRow(query.AccountId, row, existingFingerprintSet, possibleDuplicates, rules, subscriptions, pendingOccurrences))
            .ToList();

        return new PreviewImportResponse(
            query.HouseholdId,
            query.AccountId,
            ImportFingerprint.CreatePreviewFingerprint(query.AccountId, query.Rows),
            rows);
    }

    private static ImportRowResponse EnrichRow(
        Guid accountId,
        NormalizedImportRowRequest row,
        HashSet<string> existingFingerprintSet,
        IReadOnlyList<PossibleDuplicate> possibleDuplicates,
        IReadOnlyList<CategorizationRule> rules,
        IReadOnlyList<Subscription> subscriptions,
        IReadOnlyList<PendingRecurringBillOccurrence> pendingOccurrences)
    {
        var errors = ValidateRow(row);
        var description = row.Description?.Trim();
        string rowFingerprint = string.Empty;
        string duplicateState = "None";
        MoneyDto? amount = null;
        MoneyDto? balanceAfter = null;

        if (row.OccurredOn is not null && row.Amount is not null && !string.IsNullOrWhiteSpace(description))
        {
            rowFingerprint = ImportFingerprint.CreateRowFingerprint(accountId, row.OccurredOn.Value, row.Amount.Value, description);
            if (existingFingerprintSet.Contains(rowFingerprint))
            {
                duplicateState = "Exact";
            }
            else if (possibleDuplicates.Any(x => x.OccurredOn == row.OccurredOn.Value && x.Amount == Math.Abs(row.Amount.Value)))
            {
                duplicateState = "Possible";
            }

            amount = new MoneyDto(Math.Abs(decimal.Round(row.Amount.Value, 2, MidpointRounding.AwayFromZero)), NormalizeCurrency(row.Currency));
        }

        if (row.BalanceAfter is not null)
        {
            balanceAfter = new MoneyDto(row.BalanceAfter.Amount, row.BalanceAfter.Currency);
        }

        var suggestedCategoryId = row.CategoryId;
        if (suggestedCategoryId is null && !string.IsNullOrWhiteSpace(description))
        {
            suggestedCategoryId = rules.FirstOrDefault(rule => rule.Matches(description))?.TargetCategoryId.Value;
        }

        var suggestedSubscriptionMatches = row.OccurredOn is not null && row.Amount is not null && !string.IsNullOrWhiteSpace(description)
            ? SuggestSubscriptionMatches(subscriptions, row.OccurredOn.Value, Math.Abs(row.Amount.Value), description)
            : [];
        var suggestedRecurringBillMatches = row.OccurredOn is not null && row.Amount is not null
            ? SuggestRecurringBillMatches(pendingOccurrences, row.OccurredOn.Value, row.Amount.Value, description)
            : [];

        return new ImportRowResponse(
            row.RowNumber,
            row.OccurredOn,
            amount,
            description,
            NormalizeCurrency(row.Currency),
            row.Counterparty,
            row.Reference,
            balanceAfter,
            row.RawDescription,
            suggestedCategoryId,
            row.CategoryId,
            duplicateState,
            rowFingerprint,
            suggestedSubscriptionMatches,
            suggestedRecurringBillMatches,
            errors);
    }

    private static List<SubscriptionMatchSuggestionResponse> SuggestSubscriptionMatches(
        IReadOnlyList<Subscription> subscriptions,
        DateOnly occurredOn,
        decimal amount,
        string description)
    {
        return subscriptions
            .Select(subscription => new
            {
                Subscription = subscription,
                Match = SubscriptionChargeMatcher.Match(subscription, occurredOn, amount, description)
            })
            .Where(candidate => candidate.Match is not null)
            .OrderBy(candidate => candidate.Match!.DayDelta)
            .ThenBy(candidate => candidate.Match!.AmountDelta)
            .Take(3)
            .Select(candidate => new SubscriptionMatchSuggestionResponse(
                candidate.Subscription.Id.Value,
                candidate.Subscription.Name,
                "suggested",
                MoneyContract.From(candidate.Subscription.ExpectedAmount)))
            .ToList();
    }

    private static List<RecurringBillMatchSuggestionResponse> SuggestRecurringBillMatches(
        IReadOnlyList<PendingRecurringBillOccurrence> pendingOccurrences,
        DateOnly occurredOn,
        decimal signedAmount,
        string? description)
    {
        var kind = signedAmount < 0 ? TransactionKind.Expense : TransactionKind.Income;
        var amount = Math.Abs(signedAmount);
        return pendingOccurrences
            .Where(candidate => candidate.Bill.Direction.ToTransactionKind() == kind)
            .Select(candidate => new
            {
                Candidate = candidate,
                DayDelta = Math.Abs(candidate.Occurrence.DueOn.DayNumber - occurredOn.DayNumber),
                AmountDelta = Math.Abs(candidate.Bill.Amount.Amount - amount),
                TextMatch = TextMatches(candidate.Bill, description)
            })
            .Where(candidate => candidate.DayDelta <= 7)
            .Where(candidate => candidate.AmountDelta <= Math.Max(100m, candidate.Candidate.Bill.Amount.Amount * 0.5m) ||
                                candidate.TextMatch)
            .OrderByDescending(candidate => candidate.TextMatch)
            .ThenBy(candidate => candidate.DayDelta)
            .ThenBy(candidate => candidate.AmountDelta)
            .Take(3)
            .Select(candidate => new RecurringBillMatchSuggestionResponse(
                candidate.Candidate.Bill.Id.Value,
                candidate.Candidate.Occurrence.Id,
                candidate.Candidate.Bill.Name,
                candidate.Candidate.Occurrence.DueOn,
                MoneyContract.From(candidate.Candidate.Bill.Amount),
                candidate.DayDelta,
                decimal.Round(candidate.AmountDelta, 2, MidpointRounding.AwayFromZero)))
            .ToList();
    }

    private static bool TextMatches(RecurringBill bill, string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        return Contains(description, bill.Name) ||
               (!string.IsNullOrWhiteSpace(bill.Note) && Contains(description, bill.Note));
    }

    private static bool Contains(string value, string pattern) =>
        value.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
        pattern.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static List<ImportRowValidationErrorResponse> ValidateRow(NormalizedImportRowRequest row)
    {
        var errors = new List<ImportRowValidationErrorResponse>();
        if (row.OccurredOn is null)
        {
            errors.Add(new ImportRowValidationErrorResponse(nameof(row.OccurredOn), "OccurredOn is required."));
        }

        if (row.Amount is null || row.Amount.Value == 0)
        {
            errors.Add(new ImportRowValidationErrorResponse(nameof(row.Amount), "Amount is required and cannot be zero."));
        }

        if (string.IsNullOrWhiteSpace(row.Description))
        {
            errors.Add(new ImportRowValidationErrorResponse(nameof(row.Description), "Description is required."));
        }

        var currency = NormalizeCurrency(row.Currency);
        if (currency.Length != 3 || currency.Any(c => c is < 'A' or > 'Z'))
        {
            errors.Add(new ImportRowValidationErrorResponse(nameof(row.Currency), "Currency must be a three-letter ISO code."));
        }

        return errors;
    }

    private static string NormalizeCurrency(string? currency) =>
        string.IsNullOrWhiteSpace(currency) ? "SEK" : currency.Trim().ToUpperInvariant();

    private sealed record PossibleDuplicate(DateOnly OccurredOn, decimal Amount);

    private sealed record PendingRecurringBillOccurrence(RecurringBill Bill, RecurringBillOccurrence Occurrence);
}
