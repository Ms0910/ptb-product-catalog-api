namespace ProductCatalog.Application.Products.Contracts;

/// <summary>Resultado de un ajuste de stock.</summary>
/// <param name="ProductId">Producto ajustado.</param>
/// <param name="MovementId">Identificador del movimiento de stock registrado en el historial.</param>
/// <param name="Operation">Operación aplicada.</param>
/// <param name="Quantity">Unidades agregadas o quitadas.</param>
/// <param name="PreviousStock">Stock antes del ajuste.</param>
/// <param name="CurrentStock">Stock después del ajuste.</param>
/// <param name="OccurredAt">Cuándo se aplicó el ajuste (UTC).</param>
public sealed record StockAdjustmentResponse(
    Guid ProductId,
    Guid MovementId,
    StockOperation Operation,
    int Quantity,
    int PreviousStock,
    int CurrentStock,
    DateTimeOffset OccurredAt);
