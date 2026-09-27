using ProductCatalog.Application.Common.Pagination;
using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Application.Products;

public interface IProductService
{
    /// <param name="request">Datos del producto a crear.</param>
    /// <param name="actor">Identificador de quién crea el producto (desde <c>X-Actor</c>).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<ProductResponse> CreateAsync(CreateProductRequest request, string actor, CancellationToken cancellationToken);

    Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductResponse>> ListAsync(ProductListQuery query, CancellationToken cancellationToken);

    /// <param name="id">Producto a actualizar.</param>
    /// <param name="request">Nuevos datos del producto.</param>
    /// <param name="expectedVersion">Versión leída por el cliente (desde <c>If-Match</c>); <c>null</c> omite la verificación.</param>
    /// <param name="actor">Identificador de quién actualiza el producto (desde <c>X-Actor</c>).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, uint? expectedVersion, string actor, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <param name="id">Producto cuyo stock cambia.</param>
    /// <param name="request">Operación y cantidad.</param>
    /// <param name="idempotencyKey">Clave opcional del cliente; los reintentos con la misma clave se aplican una sola vez.</param>
    /// <param name="actor">Identificador de quién ajusta el stock (desde <c>X-Actor</c>).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<StockAdjustmentResult> AdjustStockAsync(Guid id, AdjustStockRequest request, string? idempotencyKey, string actor, CancellationToken cancellationToken);

    Task<PagedResult<StockMovementResponse>> ListStockMovementsAsync(Guid id, StockMovementListQuery query, CancellationToken cancellationToken);
}
