using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Application.Products;

namespace ProductCatalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IProductService, ProductService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}
