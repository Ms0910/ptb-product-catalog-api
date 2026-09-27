using FluentValidation;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Products.Validators;

internal sealed class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public const int MaxQuantityPerOperation = 1_000_000;

    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.Operation).IsInEnum();
        RuleFor(x => x.Quantity).InclusiveBetween(1, MaxQuantityPerOperation);
        RuleFor(x => x.Reason).MaximumLength(StockMovement.ReasonMaxLength);
    }
}
