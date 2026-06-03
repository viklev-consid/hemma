using System.ComponentModel.DataAnnotations;

namespace Hemma.Modules.Catalog;

public sealed class CatalogOptions
{
    [Range(1, 500)]
    public int MaxProductsPerPage { get; init; } = 100;
}
