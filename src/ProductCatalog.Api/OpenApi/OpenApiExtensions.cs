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

        return services;
    }

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
