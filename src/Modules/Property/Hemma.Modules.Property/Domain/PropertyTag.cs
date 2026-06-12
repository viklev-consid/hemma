using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Property.Domain;

public sealed class PropertyTag : AggregateRoot<PropertyTagId>
{
    private PropertyTag(PropertyTagId id, Guid householdId, string name, string? color) : base(id)
    {
        HouseholdId = householdId;
        Name = name;
        Color = color;
    }

    private PropertyTag() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Color { get; private set; }
    public bool IsArchived { get; private set; }

    public static ErrorOr<PropertyTag> Create(Guid householdId, string name, string? color)
    {
        var details = Validate(name, color);
        return details.IsError
            ? details.Errors
            : new PropertyTag(PropertyTagId.New(), householdId, details.Value.Name, details.Value.Color);
    }

    public ErrorOr<Success> Update(string name, string? color)
    {
        var details = Validate(name, color);
        if (details.IsError)
        {
            return details.Errors;
        }

        Name = details.Value.Name;
        Color = details.Value.Color;
        return Result.Success;
    }

    public void Archive() => IsArchived = true;

    private static ErrorOr<TagDetails> Validate(string name, string? color)
    {
        var normalizedName = name.Trim();
        if (normalizedName.Length is 0 or > 80)
        {
            return PropertyErrors.TagNameInvalid;
        }

        var normalizedColor = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        if (normalizedColor is { Length: > 40 })
        {
            return PropertyErrors.TagColorInvalid;
        }

        return new TagDetails(normalizedName, normalizedColor);
    }

    private sealed record TagDetails(string Name, string? Color);
}
