using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
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

                ConfigureSpanishModelBindingMessages(options.ModelBindingMessageProvider);
            })
            .AddJsonOptions(options =>
            {
                ConfigureJson(options.JsonSerializerOptions);

                // Los mensajes de System.Text.Json están en inglés; se reemplazan por los de ModelBindingMessageProvider.
                options.AllowInputFormatterExceptionMessages = false;
            });

        // El generador de OpenAPI lee estas opciones (enums como strings, números como números).
        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            if (context.ProblemDetails.Title is { } title && SpanishDefaultTitles.TryGetValue(title, out var spanishTitle))
            {
                context.ProblemDetails.Title = spanishTitle;
            }

            if (context.ProblemDetails is HttpValidationProblemDetails validation)
            {
                TranslateInvalidInputErrors(validation.Errors);
            }

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

    // Títulos por defecto que ASP.NET Core asigna a los problem details que no genera GlobalExceptionHandler.
    private static readonly Dictionary<string, string> SpanishDefaultTitles = new(StringComparer.Ordinal)
    {
        ["One or more validation errors occurred."] = "Se produjeron uno o más errores de validación.",
        ["Bad Request"] = "Solicitud incorrecta",
        ["Unauthorized"] = "No autorizado",
        ["Forbidden"] = "Prohibido",
        ["Not Found"] = "No encontrado",
        ["Method Not Allowed"] = "Método no permitido",
        ["Not Acceptable"] = "No aceptable",
        ["Request Timeout"] = "Tiempo de espera agotado",
        ["Conflict"] = "Conflicto",
        ["Precondition Failed"] = "Precondición fallida",
        ["Payload Too Large"] = "Contenido demasiado grande",
        ["Content Too Large"] = "Contenido demasiado grande",
        ["Unsupported Media Type"] = "Tipo de contenido no soportado",
        ["Unprocessable Entity"] = "Entidad no procesable",
        ["Unprocessable Content"] = "Contenido no procesable",
        ["Too Many Requests"] = "Demasiadas solicitudes",
        ["An error occurred while processing your request."] = "Ocurrió un error al procesar la solicitud.",
        ["Internal Server Error"] = "Error interno del servidor",
        ["Service Unavailable"] = "Servicio no disponible",
    };

    // Mensaje fijo que SystemTextJsonInputFormatter usa cuando AllowInputFormatterExceptionMessages es false.
    private const string InvalidInputMessage = "The input was not valid.";

    private static void TranslateInvalidInputErrors(IDictionary<string, string[]> errors)
    {
        foreach (var (key, messages) in errors)
        {
            for (var i = 0; i < messages.Length; i++)
            {
                if (messages[i] == InvalidInputMessage)
                {
                    messages[i] = key is "$" or ""
                        ? "El cuerpo de la solicitud no es un JSON válido o le faltan campos obligatorios."
                        : $"El valor de '{key.TrimStart('$', '.')}' no es válido.";
                }
            }
        }
    }

    private static void ConfigureSpanishModelBindingMessages(DefaultModelBindingMessageProvider messages)
    {
        messages.SetMissingBindRequiredValueAccessor(name => $"No se proporcionó un valor para '{name}'.");
        messages.SetMissingKeyOrValueAccessor(() => "Se requiere un valor.");
        messages.SetMissingRequestBodyRequiredValueAccessor(() => "Se requiere un cuerpo de solicitud no vacío.");
        messages.SetValueMustNotBeNullAccessor(value => $"El valor '{value}' no es válido.");
        messages.SetAttemptedValueIsInvalidAccessor((value, name) => $"El valor '{value}' no es válido para '{name}'.");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"El valor '{value}' no es válido.");
        messages.SetUnknownValueIsInvalidAccessor(name => $"El valor proporcionado para '{name}' no es válido.");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor proporcionado no es válido.");
        messages.SetValueIsInvalidAccessor(value => $"El valor '{value}' no es válido.");
        messages.SetValueMustBeANumberAccessor(name => $"El campo '{name}' debe ser un número.");
        messages.SetNonPropertyValueMustBeANumberAccessor(() => "El campo debe ser un número.");
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.NumberHandling = JsonNumberHandling.Strict;
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    }
}
