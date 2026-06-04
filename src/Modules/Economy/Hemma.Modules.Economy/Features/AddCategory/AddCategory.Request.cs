namespace Hemma.Modules.Economy.Features.AddCategory;

public sealed record AddCategoryRequest(Guid HouseholdId, string Name, Guid? ParentCategoryId, bool Budgetable);
