using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using ProductCatalog.Api.ErrorHandling;
using ProductCatalog.Api.OpenApi;
using ProductCatalog.Api.Validation;

namespace ProductCatalog.Api;

internal static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services
            .AddControllers(options =>
            {
                options.Filters.Add<FluentValidationFilter>();

                // Los campos obligatorios se declaran con miembros 'required' de C# y los valida
                // FluentValidation; esto evita errores de modelo duplicados de "field is required".
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));

        // El generador de OpenAPI lee estas opciones (enums como strings, números como números).
        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??= $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // El TLS termina en el proxy del proveedor de hosting; se confía en sus headers reenviados.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddApiDocumentation();

        return services;
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.NumberHandling = JsonNumberHandling.Strict;
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    }
}
