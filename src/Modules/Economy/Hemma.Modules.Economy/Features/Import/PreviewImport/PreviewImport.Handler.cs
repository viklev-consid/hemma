using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Economy.Features.Import.Contracts;
using Hemma.Modules.Economy.Persistence;
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

        var rows = query.Rows
            .Select(row => EnrichRow(query.AccountId, row, existingFingerprintSet, possibleDuplicates, rules, subscriptions))
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
        IReadOnlyList<Subscription> subscriptions)
    {
        var errors = ValidateRow(row);
        var description = row.Description?.Trim();
        string rowFingerprint = string.Empty;
        string duplicateState = "None";
        MoneyResponse? amount = null;
        MoneyResponse? balanceAfter = null;

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

            amount = new MoneyResponse(Math.Abs(decimal.Round(row.Amount.Value, 2, MidpointRounding.AwayFromZero)), NormalizeCurrency(row.Currency));
        }

        if (row.BalanceAfter is not null)
        {
            balanceAfter = new MoneyResponse(row.BalanceAfter.Amount, row.BalanceAfter.Currency);
        }

        var suggestedCategoryId = row.CategoryId;
        if (suggestedCategoryId is null && !string.IsNullOrWhiteSpace(description))
        {
            suggestedCategoryId = rules.FirstOrDefault(rule => rule.Matches(description))?.TargetCategoryId.Value;
        }

        var suggestedSubscriptionMatches = row.OccurredOn is not null && row.Amount is not null && !string.IsNullOrWhiteSpace(description)
            ? SuggestSubscriptionMatches(subscriptions, row.OccurredOn.Value, Math.Abs(row.Amount.Value), description)
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
                MoneyResponse.From(candidate.Subscription.ExpectedAmount)))
            .ToList();
    }

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
}
