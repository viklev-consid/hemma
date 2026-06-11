using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Contracts;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.AddCategory;

public sealed class AddCategoryHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<CategoryResponse>> Handle(AddCategoryCommand cmd, CancellationToken ct)
    {
        if (!await db.EconomySettings.AnyAsync(settings => settings.HouseholdId == cmd.HouseholdId, ct))
        {
            return EconomyErrors.SettingsNotFound;
        }

        Category? parent = null;
        if (cmd.ParentCategoryId is not null)
        {
            var parentId = new CategoryId(cmd.ParentCategoryId.Value);
            parent = await db.Categories
                .SingleOrDefaultAsync(category => category.HouseholdId == cmd.HouseholdId && category.Id == parentId, ct);
            if (parent is null)
            {
                return EconomyErrors.CategoryNotFound;
            }
        }

        var category = Category.Create(cmd.HouseholdId, cmd.Name, parent, cmd.Budgetable);
        if (category.IsError)
        {
            return category.Errors;
        }

        db.Categories.Add(category.Value);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(category.Value.HouseholdId, "economy.category.created", "Category", category.Value.Id.Value, null, ct);

        return CategoryResponse.From(category.Value);
    }
}
