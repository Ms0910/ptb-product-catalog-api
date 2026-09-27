using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Products;

internal static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product) =>
        new(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.CreatedAt,
            product.UpdatedAt,
            product.CreatedBy,
            product.UpdatedBy,
            product.Version);

    public static StockMovementResponse ToResponse(this StockMovement movement) =>
        new(
            movement.Id,
            movement.Type.ToOperation(),
            Math.Abs(movement.Quantity),
            movement.ResultingStock,
            movement.Reason,
            movement.OccurredAt,
            movement.CreatedBy);

    public static StockAdjustmentResponse ToAdjustmentResponse(this StockMovement movement) =>
        new(
            movement.ProductId,
            movement.Id,
            movement.Type.ToOperation(),
            Math.Abs(movement.Quantity),
            movement.ResultingStock - movement.Quantity,
            movement.ResultingStock,
            movement.OccurredAt);

    private static StockOperation ToOperation(this StockMovementType type) =>
        type == StockMovementType.Increase ? StockOperation.Increase : StockOperation.Decrease;
}
