using FluentValidation;
using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Application.Products.Validators;

internal sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).ValidProductName().WithName("Nombre");
        RuleFor(x => x.Description).ValidProductDescription().WithName("Descripción");
        RuleFor(x => x.Price).ValidPrice().WithName("Precio");
        RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0).WithName("Stock inicial");
    }
}
