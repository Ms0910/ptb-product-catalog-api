namespace ProductCatalog.Application.Products.Contracts;

/// <summary>Una entrada del historial de stock de un producto.</summary>
/// <param name="Id">Identificador del movimiento.</param>
/// <param name="Operation">Si se agregaron o quitaron unidades.</param>
/// <param name="Quantity">Unidades agregadas o quitadas (siempre positivo).</param>
/// <param name="ResultingStock">Stock después del movimiento.</param>
/// <param name="Reason">Nota enviada con el ajuste.</param>
/// <param name="OccurredAt">Cuándo ocurrió el movimiento (UTC).</param>
/// <param name="CreatedBy">Identificador de quién registró el movimiento.</param>
public sealed record StockMovementResponse(
    Guid Id,
    StockOperation Operation,
    int Quantity,
    int ResultingStock,
    string? Reason,
    DateTimeOffset OccurredAt,
    string CreatedBy);
