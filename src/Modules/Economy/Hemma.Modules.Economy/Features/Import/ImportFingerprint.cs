using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hemma.Modules.Economy.Features.Import.Contracts;

namespace Hemma.Modules.Economy.Features.Import;

internal static class ImportFingerprint
{
    public static string CreatePreviewFingerprint(Guid accountId, IEnumerable<NormalizedImportRowRequest> rows)
    {
        var canonicalRows = rows.Select(row => new
        {
            row.RowNumber,
            OccurredOn = row.OccurredOn?.ToString("O", CultureInfo.InvariantCulture),
            Amount = row.Amount?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            Description = NormalizeDescription(row.Description),
            Currency = NormalizeCurrency(row.Currency),
            Counterparty = NormalizeNullable(row.Counterparty),
            Reference = NormalizeNullable(row.Reference),
            BalanceAfterAmount = row.BalanceAfter?.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            BalanceAfterCurrency = NormalizeCurrency(row.BalanceAfter?.Currency),
            RawDescription = NormalizeNullable(row.RawDescription),
            row.CategoryId
        });

        return Hash(new { AccountId = accountId, Rows = canonicalRows });
    }

    public static string CreateRowFingerprint(Guid accountId, DateOnly occurredOn, decimal amount, string description)
    {
        return Hash(new
        {
            AccountId = accountId,
            OccurredOn = occurredOn.ToString("O", CultureInfo.InvariantCulture),
            Amount = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            Description = NormalizeDescription(description)
        });
    }

    public static string NormalizeDescription(string? value) =>
        string.Join(' ', (value ?? string.Empty).Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string Hash<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? NormalizeCurrency(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
