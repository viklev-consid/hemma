using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.Features.Contracts;

public sealed record CategoryResponse(
    Guid CategoryId,
    string Name,
    Guid? ParentCategoryId,
    bool Budgetable,
    IReadOnlyCollection<CategoryResponse> Children)
{
    public static CategoryResponse From(Category category, IEnumerable<CategoryResponse>? children = null) =>
        new(
            category.Id.Value,
            category.Name,
            category.ParentCategoryId?.Value,
            category.Budgetable,
            children?.ToArray() ?? []);
}
