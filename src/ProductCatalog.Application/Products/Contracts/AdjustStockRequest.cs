namespace ProductCatalog.Application.Products.Contracts;

/// <summary>Agrega o quita unidades del stock actual de un producto.</summary>
public sealed record AdjustStockRequest
{
    /// <summary>Si las unidades se agregan (<c>Increase</c>) o se quitan (<c>Decrease</c>).</summary>
    /// <example>Decrease</example>
    public required StockOperation Operation { get; init; }

    /// <summary>Cantidad de unidades, siempre positiva (1 a 1.000.000).</summary>
    /// <example>3</example>
    public required int Quantity { get; init; }

    /// <summary>Nota opcional almacenada en el historial de stock (máximo 250 caracteres).</summary>
    /// <example>Order #1234</example>
    public string? Reason { get; init; }
}
