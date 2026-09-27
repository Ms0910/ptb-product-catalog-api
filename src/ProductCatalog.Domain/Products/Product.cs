using ProductCatalog.Domain.Common;

namespace ProductCatalog.Domain.Products;

/// <summary>
/// Todo cambio de estado pasa por un método que
/// protege los invariantes, de modo que un producto nunca puede quedar con stock
/// negativo ni con un precio inválido, sin importar qué caso de uso la toque.
/// </summary>
public sealed class Product
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const decimal MaxPrice = 9_999_999_999.99m;

    // EF Core la necesita para materializar entidades.
    private Product()
    {
    }

    private Product(Guid id, string name, string? description, decimal price, int stock, string createdBy, DateTimeOffset now)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        CreatedBy = createdBy;
        UpdatedBy = createdBy;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public int Stock { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string CreatedBy { get; private set; } = string.Empty;

    public string UpdatedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Token de concurrencia optimista. Se mapea a la columna de sistema <c>xmin</c> de
    /// PostgreSQL, así que cambia en cada actualización de la fila sin necesidad de código de aplicación.
    /// </summary>
    public uint Version { get; private set; }

    public static Product Create(string name, string? description, decimal price, int initialStock, string createdBy, DateTimeOffset now)
    {
        if (initialStock < 0)
        {
            throw new DomainException(ProductErrorCodes.InvalidStock, "Initial stock cannot be negative.");
        }

        return new Product(
            Guid.CreateVersion7(now),
            NormalizeName(name),
            NormalizeDescription(description),
            EnsureValidPrice(price),
            initialStock,
            NormalizeActor(createdBy),
            now);
    }

    /// <summary>
    /// Actualiza los datos descriptivos del producto. El stock queda deliberadamente fuera de
    /// esta operación: solo cambia a través de <see cref="IncreaseStock"/> y
    /// <see cref="DecreaseStock"/>, así una actualización completa nunca puede sobrescribir
    /// unidades que otro cliente haya reservado mientras tanto.
    /// </summary>
    public void UpdateDetails(string name, string? description, decimal price, string updatedBy, DateTimeOffset now)
    {
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
        Price = EnsureValidPrice(price);
        UpdatedBy = NormalizeActor(updatedBy);
        UpdatedAt = now;
    }

    public StockMovement IncreaseStock(int quantity, string? reason, string? idempotencyKey, string updatedBy, DateTimeOffset now)
    {
        EnsurePositiveQuantity(quantity);

        if ((long)Stock + quantity > int.MaxValue)
        {
            throw new DomainException(ProductErrorCodes.StockOverflow, "The resulting stock exceeds the maximum allowed value.");
        }

        Stock += quantity;
        UpdatedBy = NormalizeActor(updatedBy);
        UpdatedAt = now;

        return StockMovement.Record(this, quantity, reason, idempotencyKey, UpdatedBy, now);
    }

    public StockMovement DecreaseStock(int quantity, string? reason, string? idempotencyKey, string updatedBy, DateTimeOffset now)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > Stock)
        {
            throw new InsufficientStockException(Id, Stock, quantity);
        }

        Stock -= quantity;
        UpdatedBy = NormalizeActor(updatedBy);
        UpdatedAt = now;

        return StockMovement.Record(this, -quantity, reason, idempotencyKey, UpdatedBy, now);
    }

    private static string NormalizeName(string name)
    {
        var normalized = name?.Trim();

        if (string.IsNullOrEmpty(normalized))
        {
            throw new DomainException(ProductErrorCodes.InvalidName, "Product name is required.");
        }

        if (normalized.Length > NameMaxLength)
        {
            throw new DomainException(ProductErrorCodes.InvalidName, $"Product name cannot exceed {NameMaxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        var normalized = description?.Trim();

        if (normalized?.Length > DescriptionMaxLength)
        {
            throw new DomainException(ProductErrorCodes.InvalidDescription, $"Product description cannot exceed {DescriptionMaxLength} characters.");
        }

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static decimal EnsureValidPrice(decimal price)
    {
        if (price <= 0 || price > MaxPrice)
        {
            throw new DomainException(ProductErrorCodes.InvalidPrice, $"Price must be greater than 0 and at most {MaxPrice}.");
        }

        if (decimal.Round(price, 2) != price)
        {
            throw new DomainException(ProductErrorCodes.InvalidPrice, "Price cannot have more than 2 decimal places.");
        }

        // Normaliza la escala a 2 decimales (5 -> 5.00) para que el valor sea idéntico antes y después de persistirlo.
        return decimal.Round(price, 2) + 0.00m;
    }

    private static string NormalizeActor(string actor)
    {
        var normalized = actor?.Trim();

        if (string.IsNullOrEmpty(normalized))
        {
            throw new DomainException(ProductErrorCodes.InvalidActor, "Actor is required.");
        }

        return normalized;
    }

    private static void EnsurePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException(ProductErrorCodes.InvalidQuantity, "Quantity must be greater than 0.");
        }
    }
}
