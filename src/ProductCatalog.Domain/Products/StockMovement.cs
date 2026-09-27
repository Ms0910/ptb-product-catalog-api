namespace ProductCatalog.Domain.Products;

/// <summary>
/// Registro de auditoría inmutable de un cambio de stock. La suma de los movimientos de un
/// producto siempre es igual a su stock actual, lo que hace el inventario trazable y permite
/// que la API reproduzca solicitudes idempotentes.
/// </summary>
public sealed class StockMovement
{
    public const int ReasonMaxLength = 250;
    public const int IdempotencyKeyMaxLength = 100;

    // EF Core la necesita para materializar entidades.
    private StockMovement()
    {
    }

    private StockMovement(Guid productId, int quantity, int resultingStock, string? reason, string? idempotencyKey, string createdBy, DateTimeOffset occurredAt)
    {
        Id = Guid.CreateVersion7(occurredAt);
        ProductId = productId;
        Quantity = quantity;
        ResultingStock = resultingStock;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        IdempotencyKey = idempotencyKey;
        CreatedBy = createdBy;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    /// <summary>Número de unidades con signo: positivo para un aumento, negativo para una disminución.</summary>
    public int Quantity { get; private set; }

    public int ResultingStock { get; private set; }

    public string? Reason { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public string CreatedBy { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public StockMovementType Type => Quantity >= 0 ? StockMovementType.Increase : StockMovementType.Decrease;

    /// <summary>Registra el stock con el que se creó un producto.</summary>
    public static StockMovement InitialStock(Product product, string createdBy, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(product);
        return new StockMovement(product.Id, product.Stock, product.Stock, "Stock inicial", null, createdBy, now);
    }

    internal static StockMovement Record(Product product, int quantity, string? reason, string? idempotencyKey, string createdBy, DateTimeOffset now) =>
        new(product.Id, quantity, product.Stock, reason, idempotencyKey, createdBy, now);
}
