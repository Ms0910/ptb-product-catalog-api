using FluentValidation.TestHelper;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Application.Products.Validators;

namespace ProductCatalog.Application.Tests.Products;

public sealed class ValidatorTests
{
    [Fact]
    public void CreateProduct_WithValidData_HasNoErrors()
    {
        var result = new CreateProductRequestValidator().TestValidate(new CreateProductRequest { Name = "Keyboard", Description = "Desc", Price = 10.50m, InitialStock = 0 });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateProduct_WithInvalidData_ReportsEveryField()
    {
        var result = new CreateProductRequestValidator().TestValidate(
            new CreateProductRequest { Name = " ", Description = new string('x', 2001), Price = 10.555m, InitialStock = -1 });

        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldHaveValidationErrorFor(x => x.Price);
        result.ShouldHaveValidationErrorFor(x => x.InitialStock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void UpdateProduct_WithNonPositivePrice_Fails(decimal price)
    {
        new UpdateProductRequestValidator().TestValidate(new UpdateProductRequest { Name = "Keyboard", Description = null, Price = price })
            .ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(AdjustStockRequestValidator.MaxQuantityPerOperation + 1)]
    public void AdjustStock_WithQuantityOutOfRange_Fails(int quantity)
    {
        new AdjustStockRequestValidator().TestValidate(new AdjustStockRequest { Operation = StockOperation.Decrease, Quantity = quantity, Reason = null })
            .ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void AdjustStock_WithUndefinedOperation_Fails()
    {
        new AdjustStockRequestValidator().TestValidate(new AdjustStockRequest { Operation = (StockOperation)42, Quantity = 1, Reason = null })
            .ShouldHaveValidationErrorFor(x => x.Operation);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void ProductListQuery_WithInvalidPaging_Fails(int page, int pageSize)
    {
        new ProductListQueryValidator().TestValidate(new ProductListQuery { Page = page, PageSize = pageSize })
            .IsValid.ShouldBeFalse();
    }
}
