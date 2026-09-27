using ProductCatalog.Application.Abstractions;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Application.Common.Pagination;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Products;

internal sealed class ProductService(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IProductService
{
    private const string ProductResource = "Producto";

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, string actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = UtcNow();
        var product = Product.Create(request.Name, request.Description, request.Price, request.InitialStock, actor, now);

        repository.Add(product);

        if (product.Stock > 0)
        {
            repository.AddMovement(StockMovement.InitialStock(product, actor, now));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToResponse();
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdReadOnlyAsync(id, cancellationToken)
            ?? throw new NotFoundException(ProductResource, id);

        return product.ToResponse();
    }

    public async Task<PagedResult<ProductResponse>> ListAsync(ProductListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = await repository.ListAsync(query, cancellationToken);

        return page.Map(product => product.ToResponse());
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, uint? expectedVersion, string actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(ProductResource, id);

        // Verificación temprana contra la versión que leyó el cliente. La ventana entre esta
        // lectura y el UPDATE sigue estando cubierta por el token de concurrencia xmin.
        if (expectedVersion is not null && expectedVersion != product.Version)
        {
            throw new PreconditionFailedException();
        }

        product.UpdateDetails(request.Name, request.Description, request.Price, actor, UtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(ProductResource, id);

        repository.Remove(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<StockAdjustmentResult> AdjustStockAsync(Guid id, AdjustStockRequest request, string? idempotencyKey, string actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                // El row lock serializa los ajustes concurrentes sobre el mismo producto, así la
                // regla de dominio (stock >= 0) siempre se evalúa contra el último stock confirmado.
                var product = await repository.GetByIdForUpdateAsync(id, token)
                    ?? throw new NotFoundException(ProductResource, id);

                if (idempotencyKey is not null)
                {
                    var previous = await repository.FindMovementByIdempotencyKeyAsync(idempotencyKey, token);

                    if (previous is not null)
                    {
                        return Replay(previous, id, request);
                    }
                }

                var now = UtcNow();
                var movement = request.Operation == StockOperation.Increase
                    ? product.IncreaseStock(request.Quantity, request.Reason, idempotencyKey, actor, now)
                    : product.DecreaseStock(request.Quantity, request.Reason, idempotencyKey, actor, now);

                repository.AddMovement(movement);

                await unitOfWork.SaveChangesAsync(token);

                return new StockAdjustmentResult(movement.ToAdjustmentResponse(), IsReplay: false);
            },
            cancellationToken);
    }

    public async Task<PagedResult<StockMovementResponse>> ListStockMovementsAsync(Guid id, StockMovementListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!await repository.ExistsAsync(id, cancellationToken))
        {
            throw new NotFoundException(ProductResource, id);
        }

        var page = await repository.ListMovementsAsync(id, query.Page, query.PageSize, cancellationToken);

        return page.Map(movement => movement.ToResponse());
    }

    // PostgreSQL almacena los timestamps con precisión de microsegundos; truncar aquí hace que
    // los valores devueltos justo después de escribir sean idénticos a los que se leen después.
    private DateTimeOffset UtcNow()
    {
        var now = timeProvider.GetUtcNow();
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }

    private static StockAdjustmentResult Replay(StockMovement previous, Guid productId, AdjustStockRequest request)
    {
        var response = previous.ToAdjustmentResponse();

        var samePayload = previous.ProductId == productId
            && response.Operation == request.Operation
            && response.Quantity == request.Quantity;

        if (!samePayload)
        {
            throw new UnprocessableRequestException(
                "idempotency_key_reused",
                "La Idempotency-Key ya fue usada con una solicitud distinta.");
        }

        return new StockAdjustmentResult(response, IsReplay: true);
    }
}
