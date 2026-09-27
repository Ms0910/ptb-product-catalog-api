using FluentValidation;
using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Application.Products.Validators;

internal sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).ValidProductName().WithName("Nombre");
        RuleFor(x => x.Description).ValidProductDescription().WithName("Descripción");
        RuleFor(x => x.Price).ValidPrice().WithName("Precio");
    }
}
