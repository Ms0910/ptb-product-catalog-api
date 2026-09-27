namespace ProductCatalog.Application.Products.Contracts;

/// <summary>
/// Reemplazo completo de los datos descriptivos de un producto. El stock no forma parte de
/// este contrato; solo cambia a través del endpoint de stock.
/// </summary>
public sealed record UpdateProductRequest
{
    /// <summary>Nombre del producto (obligatorio, máximo 200 caracteres).</summary>
    /// <example>Mechanical keyboard</example>
    public required string Name { get; init; }

    /// <summary>Descripción opcional (máximo 2000 caracteres). Omitirla borra la descripción.</summary>
    /// <example>Wireless mechanical keyboard with red switches</example>
    public string? Description { get; init; }

    /// <summary>Precio unitario, mayor a 0 y con máximo 2 decimales.</summary>
    /// <example>179.90</example>
    public required decimal Price { get; init; }
}
