using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Economy.Features.Import.Contracts;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.Import.CommitImport;

public sealed class CommitImportHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<CommitImportResponse>> Handle(CommitImportCommand cmd, CancellationToken ct)
    {
        var expectedFingerprint = ImportFingerprint.CreatePreviewFingerprint(cmd.AccountId, cmd.Rows);
        if (!string.Equals(expectedFingerprint, cmd.PreviewFingerprint, StringComparison.Ordinal))
        {
            return EconomyErrors.ImportFingerprintMismatch;
        }

        var account = await db.Accounts.SingleOrDefaultAsync(
            x => x.HouseholdId == cmd.HouseholdId && x.Id == new AccountId(cmd.AccountId),
            ct);
        if (account is null)
        {
            return EconomyErrors.AccountNotFound;
        }

        var existingFingerprints = await db.Transactions
            .Where(x => x.HouseholdId == cmd.HouseholdId && x.AccountId == account.Id && x.ImportFingerprint != null)
            .Select(x => x.ImportFingerprint!)
            .ToListAsync(ct);
        var existingFingerprintSet = existingFingerprints.ToHashSet(StringComparer.Ordinal);

        var categoryIds = cmd.Rows
            .Where(x => x.CategoryId is not null)
            .Select(x => new CategoryId(x.CategoryId!.Value))
            .Distinct()
            .ToList();
        var categories = await db.Categories
            .Where(x => x.HouseholdId == cmd.HouseholdId && categoryIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var rules = await db.CategorizationRules
            .Where(x => x.HouseholdId == cmd.HouseholdId && x.Enabled)
            .OrderBy(x => x.Pattern)
            .ToListAsync(ct);

        var transactions = new List<Transaction>();
        var suggestions = new List<ImportRuleSuggestionResponse>();
        var duplicateCount = 0;

        foreach (var row in cmd.Rows)
        {
            if (row.OccurredOn is null || row.Amount is null || row.Amount.Value == 0 || string.IsNullOrWhiteSpace(row.Description))
            {
                continue;
            }

            var rowFingerprint = ImportFingerprint.CreateRowFingerprint(cmd.AccountId, row.OccurredOn.Value, row.Amount.Value, row.Description);
            if (existingFingerprintSet.Contains(rowFingerprint))
            {
                duplicateCount++;
                continue;
            }

            var currency = string.IsNullOrWhiteSpace(row.Currency) ? "SEK" : row.Currency.Trim().ToUpperInvariant();
            var amount = Money.Create(Math.Abs(row.Amount.Value), currency);
            if (amount.IsError)
            {
                return amount.Errors;
            }

            Category? category = null;
            var ruleCategory = rules.FirstOrDefault(rule => rule.Matches(row.Description))?.TargetCategoryId;
            var selectedCategoryId = row.CategoryId is not null ? new CategoryId(row.CategoryId.Value) : ruleCategory;
            if (selectedCategoryId is not null && !categories.TryGetValue(selectedCategoryId, out category))
            {
                category = await db.Categories.SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id.Equals(selectedCategoryId), ct);
                if (category is null)
                {
                    return EconomyErrors.CategoryNotFound;
                }
            }

            var kind = row.Amount.Value < 0 ? TransactionKind.Expense : TransactionKind.Income;
            var transaction = Transaction.RecordImported(
                cmd.HouseholdId,
                account,
                category,
                amount.Value,
                row.OccurredOn.Value,
                row.Description,
                kind,
                rowFingerprint);
            if (transaction.IsError)
            {
                return transaction.Errors;
            }

            transactions.Add(transaction.Value);
            existingFingerprintSet.Add(rowFingerprint);

            if (row.CategoryId is not null && ruleCategory is null)
            {
                var pattern = FirstSuggestionToken(row.Description);
                if (pattern is not null && !suggestions.Any(x => string.Equals(x.Pattern, pattern, StringComparison.OrdinalIgnoreCase) && x.TargetCategoryId == row.CategoryId))
                {
                    suggestions.Add(new ImportRuleSuggestionResponse(pattern, "Contains", row.CategoryId.Value));
                }
            }
        }

        db.Transactions.AddRange(transactions);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            db.ChangeTracker.Clear();
            duplicateCount += transactions.Count;
            transactions.Clear();
        }

        await audit.PublishAsync(cmd.HouseholdId, "economy.import.committed", "Account", account.Id.Value, null, ct);

        return new CommitImportResponse(
            transactions.Count,
            duplicateCount,
            transactions.Select(TransactionResponse.From).ToList(),
            suggestions);
    }

    private static string? FirstSuggestionToken(string description)
    {
        var token = description
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(x => x.Length >= 3);
        return token?.Length > 40 ? token[..40] : token;
    }
}
