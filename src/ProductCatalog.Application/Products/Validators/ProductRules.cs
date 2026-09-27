using FluentValidation;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Products.Validators;

/// <summary>
/// Reglas de entrada compartidas por los contratos de creación y actualización. Reflejan los
/// invariantes del dominio para que los clientes reciban todos los errores de una vez como un
/// 400, antes de tocar el dominio.
/// </summary>
internal static class ProductRules
{
    public static IRuleBuilderOptions<T, string> ValidProductName<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("'{PropertyName}' es obligatorio.")
            .MaximumLength(Product.NameMaxLength);

    public static IRuleBuilderOptions<T, string?> ValidProductDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(Product.DescriptionMaxLength);

    public static IRuleBuilderOptions<T, decimal> ValidPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0)
            .LessThanOrEqualTo(Product.MaxPrice)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true);
}
