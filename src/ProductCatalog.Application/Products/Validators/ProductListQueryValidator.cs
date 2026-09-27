using FluentValidation;
using ProductCatalog.Application.Common.Pagination;
using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Application.Products.Validators;

internal sealed class ProductListQueryValidator : AbstractValidator<ProductListQuery>
{
    public ProductListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("Página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize).WithName("Tamaño de página");
        RuleFor(x => x.Search).MaximumLength(100).WithName("Búsqueda");
        RuleFor(x => x.SortBy).IsInEnum().WithName("Ordenar por");
        RuleFor(x => x.SortDirection).IsInEnum().WithName("Dirección de orden");
    }
}
