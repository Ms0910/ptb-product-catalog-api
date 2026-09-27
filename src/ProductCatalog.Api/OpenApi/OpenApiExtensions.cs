using Microsoft.OpenApi;

namespace ProductCatalog.Api.OpenApi;

internal static class OpenApiExtensions
{
    public const string DocumentUrl = "/openapi/v1.json";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Product Catalog API",
                Version = "v1",
                Description = """
                    API REST para gestionar un catálogo de productos y su stock.

                    * Los errores siguen **RFC 9457 Problem Details** e incluyen un `code` estable y el `traceId`.
                    * `PATCH /api/products/{id}/stock` es seguro ante concurrencia (row lock) y admite el header `Idempotency-Key` para reintentar con seguridad.
                    * `PUT /api/products/{id}` admite concurrencia optimista mediante `ETag` / `If-Match`.
                    """,
            };

            return Task.CompletedTask;
        }));

        services.AddOpenApi(options => options.AddOperationTransformer((operation, _, _) =>
        {
            if (operation.RequestBody is { } requestBody
                && requestBody.Content?.TryGetValue("application/json", out var mediaType) == true
                && mediaType?.Schema is OpenApiSchemaReference schemaReference
                && schemaReference.Reference.Id is { } schemaId
                && RequestBodyDescriptions.TryGetValue(schemaId, out var description))
            {
                requestBody.Description = description;
            }

            return Task.CompletedTask;
        }));

        return services;
    }

    // El generador de OpenAPI toma el <param> equivocado del XML doc para describir el request
    // body cuando la acción tiene varios parámetros (bug conocido de Microsoft.AspNetCore.OpenApi
    // con XML comments en controllers); se fuerza la descripción correcta según el schema referenciado.
    private static readonly Dictionary<string, string> RequestBodyDescriptions = new(StringComparer.Ordinal)
    {
        ["CreateProductRequest"] = "Datos del producto a crear.",
        ["UpdateProductRequest"] = "Nuevos datos del producto.",
        ["AdjustStockRequest"] = "Operación y cantidad de unidades.",
    };

    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(DocumentUrl, "Product Catalog API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Product Catalog API";
            options.DisplayRequestDuration();
            options.EnableTryItOutByDefault();
        });

        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

        return app;
    }
}
