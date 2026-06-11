using ErrorOr;
using Hemma.Shared.Contracts;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.ListCategories;

public sealed class ListCategoriesHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<ListCategoriesResponse>> Handle(ListCategoriesQuery query, CancellationToken ct)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Where(category => category.HouseholdId == query.HouseholdId)
            .OrderBy(category => category.Name)
            .ToListAsync(ct);

        var childrenByParent = categories
            .Where(category => category.ParentCategoryId is not null)
            .GroupBy(category => category.ParentCategoryId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(child => CategoryResponse.From(child)).ToArray());

        var roots = categories
            .Where(category => category.ParentCategoryId is null)
            .Select(category => CategoryResponse.From(
                category,
                childrenByParent.GetValueOrDefault(category.Id.Value)))
            .ToArray();

        return new ListCategoriesResponse(roots);
    }
}
