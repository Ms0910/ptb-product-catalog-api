using ProductCatalog.Application.Common.Pagination;

namespace ProductCatalog.Application.Products.Contracts;

/// <summary>Opciones de paginación, filtrado y ordenamiento para el listado de productos.</summary>
public sealed record ProductListQuery
{
    /// <summary>Número de página, empezando en 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Elementos por página (1 a 100).</summary>
    public int PageSize { get; init; } = PaginationDefaults.DefaultPageSize;

    /// <summary>Texto buscado en el nombre del producto, sin distinguir mayúsculas/minúsculas.</summary>
    public string? Search { get; init; }

    /// <summary>Campo usado para ordenar los resultados.</summary>
    public ProductSortField SortBy { get; init; } = ProductSortField.CreatedAt;

    /// <summary>Dirección del ordenamiento.</summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;
}
