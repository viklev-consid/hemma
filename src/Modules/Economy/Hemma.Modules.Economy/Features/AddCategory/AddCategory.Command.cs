namespace Hemma.Modules.Economy.Features.AddCategory;

public sealed record AddCategoryCommand(Guid HouseholdId, string Name, Guid? ParentCategoryId, bool Budgetable);
