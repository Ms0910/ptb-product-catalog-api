using ProductCatalog.Application.Common.Pagination;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Abstractions;

public interface IProductRepository
{
    /// <summary>Carga un producto con seguimiento de cambios.</summary>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Carga un producto sin seguimiento de cambios, para casos de uso de solo lectura.</summary>
    Task<Product?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Carga un producto y toma un row lock exclusivo sobre él hasta que termine la transacción
    /// actual. Las operaciones de stock concurrentes sobre el mismo producto se serializan; las
    /// operaciones sobre productos distintos no se bloquean entre sí.
    /// </summary>
    Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Product>> ListAsync(ProductListQuery query, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    void Add(Product product);

    void Remove(Product product);

    void AddMovement(StockMovement movement);

    Task<StockMovement?> FindMovementByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task<PagedResult<StockMovement>> ListMovementsAsync(Guid productId, int page, int pageSize, CancellationToken cancellationToken);
}
