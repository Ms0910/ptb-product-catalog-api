using FluentValidation;
using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Application.Products.Validators;

internal sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).ValidProductName();
        RuleFor(x => x.Description).ValidProductDescription();
        RuleFor(x => x.Price).ValidPrice();
    }
}
