using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Application.Abstractions;
using ProductCatalog.Infrastructure.Persistence;
using ProductCatalog.Infrastructure.Persistence.Repositories;

namespace ProductCatalog.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Catalog";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = new[] { configuration.GetConnectionString(ConnectionStringName), configuration["DATABASE_URL"] }
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? throw new InvalidOperationException(
                $"Configure 'ConnectionStrings:{ConnectionStringName}' or 'DATABASE_URL' with the PostgreSQL connection string.");

        services.AddDbContext<CatalogDbContext>(options => options
            .UseNpgsql(PostgresConnectionString.Normalize(connectionString), npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHealthChecks().AddDbContextCheck<CatalogDbContext>("database", tags: ["ready"]);

        return services;
    }
}
