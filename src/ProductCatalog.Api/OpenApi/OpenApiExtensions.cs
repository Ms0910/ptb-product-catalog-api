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
                    REST API to manage a product catalog and its stock.

                    * Errors follow **RFC 9457 Problem Details** and include a stable `code` and the `traceId`.
                    * `PATCH /api/products/{id}/stock` is safe under concurrency (row lock) and supports the `Idempotency-Key` header for safe retries.
                    * `PUT /api/products/{id}` supports optimistic concurrency through `ETag` / `If-Match`.
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
