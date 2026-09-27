namespace ProductCatalog.Application.Products.Contracts;

/// <summary>Datos requeridos para crear un producto.</summary>
public sealed record CreateProductRequest
{
    /// <summary>Nombre del producto (obligatorio, máximo 200 caracteres).</summary>
    /// <example>Mechanical keyboard</example>
    public required string Name { get; init; }

    /// <summary>Descripción opcional (máximo 2000 caracteres).</summary>
    /// <example>Wireless mechanical keyboard with brown switches</example>
    public string? Description { get; init; }

    /// <summary>Precio unitario, mayor a 0 y con máximo 2 decimales.</summary>
    /// <example>199.99</example>
    public required decimal Price { get; init; }

    /// <summary>Unidades disponibles al crear el producto (0 o más).</summary>
    /// <example>25</example>
    public required int InitialStock { get; init; }
}
