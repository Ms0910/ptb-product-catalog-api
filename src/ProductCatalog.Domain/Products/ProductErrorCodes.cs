namespace ProductCatalog.Domain.Products;

public static class ProductErrorCodes
{
    public const string InvalidName = "product.invalid_name";
    public const string InvalidDescription = "product.invalid_description";
    public const string InvalidPrice = "product.invalid_price";
    public const string InvalidStock = "product.invalid_stock";
    public const string InvalidQuantity = "product.invalid_quantity";
    public const string InsufficientStock = "product.insufficient_stock";
    public const string StockOverflow = "product.stock_overflow";
    public const string InvalidActor = "product.invalid_actor";
}
