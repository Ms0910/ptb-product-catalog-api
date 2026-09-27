using ProductCatalog.Application.Common.Pagination;

namespace ProductCatalog.Application.Products.Contracts;

/// <summary>Opciones de paginación para el historial de stock.</summary>
public sealed record StockMovementListQuery
{
    /// <summary>Número de página, empezando en 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Elementos por página (1 a 100).</summary>
    public int PageSize { get; init; } = PaginationDefaults.DefaultPageSize;
}
