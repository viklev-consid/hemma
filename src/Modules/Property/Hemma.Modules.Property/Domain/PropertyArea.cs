using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Property.Domain;

public sealed class PropertyArea : AggregateRoot<PropertyAreaId>
{
    private PropertyArea(PropertyAreaId id, Guid householdId, string name, string? description, int sortOrder) : base(id)
    {
        HouseholdId = householdId;
        Name = name;
        Description = description;
        SortOrder = sortOrder;
    }

    private PropertyArea() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsArchived { get; private set; }

    public static ErrorOr<PropertyArea> Create(Guid householdId, string name, string? description, int sortOrder)
    {
        var details = Validate(name, description);
        return details.IsError
            ? details.Errors
            : new PropertyArea(PropertyAreaId.New(), householdId, details.Value.Name, details.Value.Description, sortOrder);
    }

    public ErrorOr<Success> Update(string name, string? description)
    {
        var details = Validate(name, description);
        if (details.IsError)
        {
            return details.Errors;
        }

        Name = details.Value.Name;
        Description = details.Value.Description;
        return Result.Success;
    }

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    public void Archive() => IsArchived = true;

    public void Unarchive() => IsArchived = false;

    private static ErrorOr<AreaDetails> Validate(string name, string? description)
    {
        var normalizedName = name.Trim();
        if (normalizedName.Length is 0 or > 120)
        {
            return PropertyErrors.AreaNameInvalid;
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription is { Length: > 1000 })
        {
            return PropertyErrors.AreaDescriptionInvalid;
        }

        return new AreaDetails(normalizedName, normalizedDescription);
    }

    private sealed record AreaDetails(string Name, string? Description);
}
