using ErrorOr;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.Domain;

public sealed class Category : AggregateRoot<CategoryId>
{
    private Category(CategoryId id, Guid householdId, string name, CategoryId? parentCategoryId, bool budgetable) : base(id)
    {
        HouseholdId = householdId;
        Name = name;
        ParentCategoryId = parentCategoryId;
        Budgetable = budgetable;
    }

    private Category() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public string Name { get; private set; } = null!;
    public CategoryId? ParentCategoryId { get; private set; }
    public bool Budgetable { get; private set; }

    public static ErrorOr<Category> Create(Guid householdId, string name, Category? parentCategory, bool budgetable)
    {
        if (parentCategory?.ParentCategoryId is not null)
        {
            return EconomyErrors.CategoryDepthExceeded;
        }

        var normalizedName = NormalizeName(name);
        if (normalizedName.IsError)
        {
            return normalizedName.Errors;
        }

        return new Category(CategoryId.New(), householdId, normalizedName.Value, parentCategory?.Id, budgetable);
    }

    private static ErrorOr<string> NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return EconomyErrors.CategoryNameInvalid;
        }

        var trimmed = name.Trim();
        return trimmed.Length <= 100
            ? trimmed
            : EconomyErrors.CategoryNameInvalid;
    }
}
