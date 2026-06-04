using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.ListCategories;

public sealed record ListCategoriesResponse(IReadOnlyCollection<CategoryResponse> Categories);
