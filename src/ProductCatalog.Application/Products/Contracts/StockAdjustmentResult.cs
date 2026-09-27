namespace ProductCatalog.Application.Products.Contracts;

/// <param name="Response">Datos del ajuste devueltos al cliente.</param>
/// <param name="IsReplay"><c>true</c> cuando la solicitud se respondió a partir de una llamada anterior con la misma clave de idempotencia.</param>
public sealed record StockAdjustmentResult(StockAdjustmentResponse Response, bool IsReplay);
