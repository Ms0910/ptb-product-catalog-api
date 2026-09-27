using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Api.Http;
using ProductCatalog.Application.Common.Pagination;
using ProductCatalog.Application.Products;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Api.Controllers;

/// <summary>Catálogo de productos y gestión de stock.</summary>
[ApiController]
[Route("api/products")]
[Tags("Products")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>Crea un producto.</summary>
    /// <remarks>
    /// El stock inicial queda registrado como la primera entrada del historial de stock del producto.
    /// La respuesta incluye la versión del producto en el header <c>ETag</c>.
    /// </remarks>
    /// <param name="request">Datos del producto a crear.</param>
    /// <param name="actor">Identificador opcional de quién realiza la acción, enviado en <c>X-Actor</c>; por defecto es <c>"system"</c> cuando está ausente o vacío.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="201">Producto creado. El header <c>Location</c> apunta a él.</response>
    /// <response code="400">La solicitud es inválida.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> Create(
        CreateProductRequest request,
        [FromHeader(Name = "X-Actor")] string? actor,
        CancellationToken cancellationToken)
    {
        var resolvedActor = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim();
        var product = await productService.CreateAsync(request, resolvedActor, cancellationToken);

        Response.Headers.ETag = EntityTag.From(product.Version);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Obtiene un producto por id.</summary>
    /// <response code="200">El producto. Su versión se devuelve en el header <c>ETag</c>.</response>
    /// <response code="404">El producto no existe.</response>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);

        Response.Headers.ETag = EntityTag.From(product.Version);

        return product;
    }

    /// <summary>Lista productos con paginación, búsqueda y ordenamiento.</summary>
    /// <response code="200">Una página de productos.</response>
    /// <response code="400">Parámetros de paginación u ordenamiento inválidos.</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public Task<PagedResult<ProductResponse>> List([FromQuery] ProductListQuery query, CancellationToken cancellationToken) =>
        productService.ListAsync(query, cancellationToken);

    /// <summary>Actualiza el nombre, la descripción y el precio de un producto.</summary>
    /// <remarks>
    /// El stock no se modifica en este endpoint; usar <c>PATCH /api/products/{id}/stock</c>.
    /// Enviar el <c>ETag</c> recibido en <c>If-Match</c> para asegurarse de no sobrescribir
    /// cambios hechos por alguien más mientras tanto.
    /// </remarks>
    /// <param name="id">Id del producto.</param>
    /// <param name="request">Nuevos datos del producto.</param>
    /// <param name="ifMatch">Versión del producto (ETag) leída por el cliente, opcional.</param>
    /// <param name="actor">Identificador opcional de quién realiza la acción, enviado en <c>X-Actor</c>; por defecto es <c>"system"</c> cuando está ausente o vacío.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">El producto actualizado.</response>
    /// <response code="400">La solicitud es inválida.</response>
    /// <response code="404">El producto no existe.</response>
    /// <response code="409">El producto fue modificado concurrentemente.</response>
    /// <response code="412">La versión de <c>If-Match</c> está desactualizada.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed, "application/problem+json")]
    public async Task<ActionResult<ProductResponse>> Update(
        Guid id,
        UpdateProductRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromHeader(Name = "X-Actor")] string? actor,
        CancellationToken cancellationToken)
    {
        if (!EntityTag.TryParseVersion(ifMatch, out var expectedVersion))
        {
            ModelState.AddModelError("If-Match", "The If-Match header must be an ETag returned by this API (e.g. \"1234\").");
            return ValidationProblem(ModelState);
        }

        var resolvedActor = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim();
        var product = await productService.UpdateAsync(id, request, expectedVersion, resolvedActor, cancellationToken);

        Response.Headers.ETag = EntityTag.From(product.Version);

        return product;
    }

    /// <summary>Elimina un producto y su historial de stock.</summary>
    /// <response code="204">Producto eliminado.</response>
    /// <response code="404">El producto no existe.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await productService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>Aumenta o disminuye el stock de un producto.</summary>
    /// <remarks>
    /// Los ajustes concurrentes sobre el mismo producto se serializan con un row lock, de modo que
    /// el stock nunca puede quedar negativo ni perder actualizaciones, sin importar cuántos clientes
    /// llamen a la vez.
    ///
    /// Enviar una <c>Idempotency-Key</c> única por operación lógica permite reintentar con seguridad:
    /// una solicitud repetida con la misma clave devuelve el resultado original (header
    /// <c>Idempotent-Replayed: true</c>) sin aplicar el cambio dos veces.
    /// </remarks>
    /// <param name="id">Id del producto.</param>
    /// <param name="request">Operación y cantidad de unidades.</param>
    /// <param name="idempotencyKey">Clave única opcional de la operación (máximo 100 caracteres).</param>
    /// <param name="actor">Identificador opcional de quién realiza la acción, enviado en <c>X-Actor</c>; por defecto es <c>"system"</c> cuando está ausente o vacío.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Stock actualizado.</response>
    /// <response code="400">La solicitud es inválida.</response>
    /// <response code="404">El producto no existe.</response>
    /// <response code="409">No hay suficiente stock para quitar las unidades solicitadas.</response>
    /// <response code="422">La clave de idempotencia ya fue usada con una solicitud distinta.</response>
    [HttpPatch("{id:guid}/stock")]
    [Consumes("application/json")]
    [ProducesResponseType<StockAdjustmentResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<StockAdjustmentResponse>> AdjustStock(
        Guid id,
        AdjustStockRequest request,
        [FromHeader(Name = "Idempotency-Key"), MaxLength(StockMovement.IdempotencyKeyMaxLength)] string? idempotencyKey,
        [FromHeader(Name = "X-Actor")] string? actor,
        CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        var resolvedActor = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim();

        var result = await productService.AdjustStockAsync(id, request, key, resolvedActor, cancellationToken);

        if (result.IsReplay)
        {
            Response.Headers["Idempotent-Replayed"] = "true";
        }

        return result.Response;
    }

    /// <summary>Lista el historial de stock de un producto, del más reciente al más antiguo.</summary>
    /// <response code="200">Una página de movimientos de stock.</response>
    /// <response code="400">Parámetros de paginación inválidos.</response>
    /// <response code="404">El producto no existe.</response>
    [HttpGet("{id:guid}/stock-movements")]
    [ProducesResponseType<PagedResult<StockMovementResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public Task<PagedResult<StockMovementResponse>> ListStockMovements(
        Guid id,
        [FromQuery] StockMovementListQuery query,
        CancellationToken cancellationToken) =>
        productService.ListStockMovementsAsync(id, query, cancellationToken);
}
