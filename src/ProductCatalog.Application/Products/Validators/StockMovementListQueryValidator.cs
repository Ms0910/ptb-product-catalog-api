using FluentValidation;
using ProductCatalog.Application.Common.Pagination;
using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Application.Products.Validators;

internal sealed class StockMovementListQueryValidator : AbstractValidator<StockMovementListQuery>
{
    public StockMovementListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("Página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize).WithName("Tamaño de página");
    }
}
