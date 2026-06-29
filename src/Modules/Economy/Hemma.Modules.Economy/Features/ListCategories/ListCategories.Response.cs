using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.ListCategories;

public sealed record ListCategoriesResponse(IReadOnlyCollection<CategoryResponse> Categories);
