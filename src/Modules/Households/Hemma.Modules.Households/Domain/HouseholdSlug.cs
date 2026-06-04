using ErrorOr;
using Hemma.Modules.Households.Errors;

namespace Hemma.Modules.Households.Domain;

public sealed record HouseholdSlug
{
    private HouseholdSlug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ErrorOr<HouseholdSlug> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return HouseholdsErrors.SlugEmpty;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 100)
        {
            return HouseholdsErrors.SlugTooLong;
        }

        if (!IsValidSlug(normalized))
        {
            return HouseholdsErrors.SlugInvalid;
        }

        return new HouseholdSlug(normalized);
    }

    public static ErrorOr<HouseholdSlug> FromName(string name)
    {
        var chars = new List<char>(name.Length);
        var lastWasHyphen = true;
        foreach (var c in name.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                chars.Add(c);
                lastWasHyphen = false;
                continue;
            }

            if ((char.IsWhiteSpace(c) || c == '-') && !lastWasHyphen)
            {
                chars.Add('-');
                lastWasHyphen = true;
            }
        }

        if (chars.Count > 0 && chars[^1] == '-')
        {
            chars.RemoveAt(chars.Count - 1);
        }

        return Create(new string([.. chars]));
    }

    public override string ToString() => Value;

    private static bool IsValidSlug(string value)
    {
        if (value[0] == '-' || value[^1] == '-')
        {
            return false;
        }

        var lastWasHyphen = false;
        foreach (var c in value)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                lastWasHyphen = false;
                continue;
            }

            if (c == '-' && !lastWasHyphen)
            {
                lastWasHyphen = true;
                continue;
            }

            return false;
        }

        return true;
    }
}
