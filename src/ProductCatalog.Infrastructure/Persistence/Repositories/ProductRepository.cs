using Microsoft.EntityFrameworkCore;
using ProductCatalog.Application.Abstractions;
using ProductCatalog.Application.Common.Pagination;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(CatalogDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken) =>
        context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        // se mantiene hasta que termina la transacción
        context.Products
            .FromSql($"SELECT *, xmin FROM products WHERE id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<Product>> ListAsync(ProductListQuery query, CancellationToken cancellationToken)
    {
        var products = context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            products = products.Where(p => EF.Functions.ILike(p.Name, pattern, @"\"));
        }

        var totalCount = await products.CountAsync(cancellationToken);

        var items = await Sort(products, query.SortBy, query.SortDirection)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Product>(items, query.Page, query.PageSize, totalCount);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(p => p.Id == id, cancellationToken);

    public void Add(Product product) => context.Products.Add(product);

    public void Remove(Product product) => context.Products.Remove(product);

    public void AddMovement(StockMovement movement) => context.StockMovements.Add(movement);

    public Task<StockMovement?> FindMovementByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        context.StockMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<PagedResult<StockMovement>> ListMovementsAsync(Guid productId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var movements = context.StockMovements.AsNoTracking().Where(m => m.ProductId == productId);

        var totalCount = await movements.CountAsync(cancellationToken);

        var items = await movements
            .OrderByDescending(m => m.OccurredAt)
            .ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockMovement>(items, page, pageSize, totalCount);
    }

    private static IQueryable<Product> Sort(IQueryable<Product> products, ProductSortField sortBy, SortDirection direction)
    {
        var ascending = direction == SortDirection.Asc;

        var sorted = sortBy switch
        {
            ProductSortField.Name => ascending ? products.OrderBy(p => p.Name) : products.OrderByDescending(p => p.Name),
            ProductSortField.Price => ascending ? products.OrderBy(p => p.Price) : products.OrderByDescending(p => p.Price),
            ProductSortField.Stock => ascending ? products.OrderBy(p => p.Stock) : products.OrderByDescending(p => p.Stock),
            _ => ascending ? products.OrderBy(p => p.CreatedAt) : products.OrderByDescending(p => p.CreatedAt),
        };

        // El criterio de desempate mantiene las páginas estables cuando varias filas comparten el valor de orden.
        return ascending ? sorted.ThenBy(p => p.Id) : sorted.ThenByDescending(p => p.Id);
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}
