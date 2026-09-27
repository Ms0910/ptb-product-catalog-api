using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProductCatalog.Api;
using ProductCatalog.Api.OpenApi;
using ProductCatalog.Application;
using ProductCatalog.Infrastructure;
using ProductCatalog.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Los proveedores PaaS (Render, Railway, Heroku...) indican el puerto a usar a través de PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();

app.UseApiDocumentation();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = WriteHealthResponse });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = WriteHealthResponse });

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await DatabaseMigrator.MigrateAsync(app.Services);
}

await app.RunAsync();

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "text/plain; charset=utf-8";

    return context.Response.WriteAsync(report.Status switch
    {
        HealthStatus.Healthy => "Saludable",
        HealthStatus.Degraded => "Degradado",
        _ => "No saludable",
    });
}

/// <summary>Punto de entrada, expuesto para las pruebas de integración (<c>WebApplicationFactory</c>).</summary>
public partial class Program;
