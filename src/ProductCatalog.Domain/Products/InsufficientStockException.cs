using ProductCatalog.Domain.Common;

namespace ProductCatalog.Domain.Products;

public sealed class InsufficientStockException : DomainException
{
    public InsufficientStockException(Guid productId, int availableStock, int requestedQuantity)
        : base(
            ProductErrorCodes.InsufficientStock,
            $"Stock insuficiente para el producto '{productId}'. Disponible: {availableStock}, solicitado: {requestedQuantity}.")
    {
        ProductId = productId;
        AvailableStock = availableStock;
        RequestedQuantity = requestedQuantity;
    }

    public InsufficientStockException()
    {
    }

    public InsufficientStockException(string message)
        : base(message)
    {
    }

    public InsufficientStockException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public Guid ProductId { get; }

    public int AvailableStock { get; }

    public int RequestedQuantity { get; }
}
