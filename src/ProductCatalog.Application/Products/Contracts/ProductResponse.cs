namespace ProductCatalog.Application.Products.Contracts;

/// <summary>Un producto del catálogo.</summary>
/// <param name="Id">Identificador del producto (UUID v7).</param>
/// <param name="Name">Nombre del producto.</param>
/// <param name="Description">Descripción del producto.</param>
/// <param name="Price">Precio unitario.</param>
/// <param name="Stock">Unidades actualmente disponibles.</param>
/// <param name="CreatedAt">Marca de tiempo de creación (UTC).</param>
/// <param name="UpdatedAt">Marca de tiempo de la última modificación (UTC).</param>
/// <param name="CreatedBy">Identificador de quién creó el producto.</param>
/// <param name="UpdatedBy">Identificador de quién actualizó el producto por última vez.</param>
/// <param name="Version">Versión de concurrencia. También se devuelve en el header <c>ETag</c>; enviarla en <c>If-Match</c> evita sobrescribir cambios concurrentes.</param>
public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy,
    uint Version);
