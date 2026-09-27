using ProductCatalog.Domain.Common;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Domain.UnitTests.Products;

public sealed class ProductTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_InitializesProduct()
    {
        var product = Product.Create("  Keyboard  ", "  Mechanical  ", 199.99m, 10, "tester", Now);

        product.Id.ShouldNotBe(Guid.Empty);
        product.Name.ShouldBe("Keyboard");
        product.Description.ShouldBe("Mechanical");
        product.Price.ShouldBe(199.99m);
        product.Stock.ShouldBe(10);
        product.CreatedAt.ShouldBe(Now);
        product.UpdatedAt.ShouldBe(Now);
        product.CreatedBy.ShouldBe("tester");
        product.UpdatedBy.ShouldBe("tester");
    }

    [Fact]
    public void Create_WithBlankDescription_StoresNull()
    {
        var product = Product.Create("Keyboard", "   ", 10m, 0, "tester", Now);

        product.Description.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutName_Throws(string name)
    {
        var exception = Should.Throw<DomainException>(() => Product.Create(name, null, 10m, 1, "tester", Now));

        exception.Code.ShouldBe(ProductErrorCodes.InvalidName);
    }

    [Fact]
    public void Create_WithTooLongName_Throws()
    {
        var name = new string('a', Product.NameMaxLength + 1);

        Should.Throw<DomainException>(() => Product.Create(name, null, 10m, 1, "tester", Now))
            .Code.ShouldBe(ProductErrorCodes.InvalidName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.999)]
    public void Create_WithInvalidPrice_Throws(decimal price)
    {
        Should.Throw<DomainException>(() => Product.Create("Keyboard", null, price, 1, "tester", Now))
            .Code.ShouldBe(ProductErrorCodes.InvalidPrice);
    }

    [Fact]
    public void Create_WithNegativeStock_Throws()
    {
        Should.Throw<DomainException>(() => Product.Create("Keyboard", null, 10m, -1, "tester", Now))
            .Code.ShouldBe(ProductErrorCodes.InvalidStock);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutActor_Throws(string actor)
    {
        Should.Throw<DomainException>(() => Product.Create("Keyboard", null, 10m, 1, actor, Now))
            .Code.ShouldBe(ProductErrorCodes.InvalidActor);
    }

    [Fact]
    public void UpdateDetails_ChangesDataButKeepsStock()
    {
        var product = Product.Create("Keyboard", null, 10m, 7, "tester", Now);
        var later = Now.AddMinutes(5);

        product.UpdateDetails("Mouse", "Wireless", 20m, "editor", later);

        product.Name.ShouldBe("Mouse");
        product.Description.ShouldBe("Wireless");
        product.Price.ShouldBe(20m);
        product.Stock.ShouldBe(7);
        product.UpdatedAt.ShouldBe(later);
        product.CreatedAt.ShouldBe(Now);
        product.UpdatedBy.ShouldBe("editor");
        product.CreatedBy.ShouldBe("tester");
    }

    [Fact]
    public void IncreaseStock_AddsUnitsAndRecordsMovement()
    {
        var product = Product.Create("Keyboard", null, 10m, 5, "tester", Now);

        var movement = product.IncreaseStock(3, "Restock", "key-1", "stocker", Now);

        product.Stock.ShouldBe(8);
        product.UpdatedBy.ShouldBe("stocker");
        movement.ProductId.ShouldBe(product.Id);
        movement.Quantity.ShouldBe(3);
        movement.ResultingStock.ShouldBe(8);
        movement.Type.ShouldBe(StockMovementType.Increase);
        movement.Reason.ShouldBe("Restock");
        movement.IdempotencyKey.ShouldBe("key-1");
        movement.CreatedBy.ShouldBe("stocker");
    }

    [Fact]
    public void DecreaseStock_SubtractsUnitsAndRecordsNegativeMovement()
    {
        var product = Product.Create("Keyboard", null, 10m, 5, "tester", Now);

        var movement = product.DecreaseStock(5, "Sale", null, "stocker", Now);

        product.Stock.ShouldBe(0);
        movement.Quantity.ShouldBe(-5);
        movement.ResultingStock.ShouldBe(0);
        movement.Type.ShouldBe(StockMovementType.Decrease);
        movement.CreatedBy.ShouldBe("stocker");
    }

    [Fact]
    public void DecreaseStock_BeyondAvailable_ThrowsAndKeepsStock()
    {
        var product = Product.Create("Keyboard", null, 10m, 2, "tester", Now);

        var exception = Should.Throw<InsufficientStockException>(() => product.DecreaseStock(3, null, null, "stocker", Now));

        exception.AvailableStock.ShouldBe(2);
        exception.RequestedQuantity.ShouldBe(3);
        exception.Code.ShouldBe(ProductErrorCodes.InsufficientStock);
        product.Stock.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void StockChanges_WithNonPositiveQuantity_Throw(int quantity)
    {
        var product = Product.Create("Keyboard", null, 10m, 2, "tester", Now);

        Should.Throw<DomainException>(() => product.IncreaseStock(quantity, null, null, "stocker", Now)).Code.ShouldBe(ProductErrorCodes.InvalidQuantity);
        Should.Throw<DomainException>(() => product.DecreaseStock(quantity, null, null, "stocker", Now)).Code.ShouldBe(ProductErrorCodes.InvalidQuantity);
    }

    [Fact]
    public void IncreaseStock_BeyondIntRange_Throws()
    {
        var product = Product.Create("Keyboard", null, 10m, int.MaxValue, "tester", Now);

        Should.Throw<DomainException>(() => product.IncreaseStock(1, null, null, "stocker", Now))
            .Code.ShouldBe(ProductErrorCodes.StockOverflow);
    }

    [Fact]
    public void InitialStock_RecordsCreationStock()
    {
        var product = Product.Create("Keyboard", null, 10m, 12, "tester", Now);

        var movement = StockMovement.InitialStock(product, "tester", Now);

        movement.Quantity.ShouldBe(12);
        movement.ResultingStock.ShouldBe(12);
        movement.Reason.ShouldBe("Initial stock");
        movement.CreatedBy.ShouldBe("tester");
    }
}
