using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class CategoryTests
{
    [Fact]
    public void Create_WhenParentAlreadyHasParent_ReturnsDepthError()
    {
        var householdId = Guid.NewGuid();
        var root = Category.Create(householdId, "Root", parentCategory: null, budgetable: false).Value;
        var child = Category.Create(householdId, "Child", root, budgetable: true).Value;

        var result = Category.Create(householdId, "Grandchild", child, budgetable: true);

        Assert.True(result.IsError);
    }
}
