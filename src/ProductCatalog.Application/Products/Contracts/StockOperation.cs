namespace ProductCatalog.Application.Products.Contracts;

public enum StockOperation
{
    /// <summary>Agrega unidades al stock actual.</summary>
    Increase,

    /// <summary>Quita unidades del stock actual. Falla si no hay unidades suficientes.</summary>
    Decrease,
}
