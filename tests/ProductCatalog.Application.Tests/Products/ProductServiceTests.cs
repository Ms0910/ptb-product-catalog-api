using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ProductCatalog.Application.Abstractions;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Application.Products;
using ProductCatalog.Application.Products.Contracts;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Tests.Products;

public sealed class ProductServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<StockAdjustmentResult>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<StockAdjustmentResult>>>()(CancellationToken.None));

        _sut = new ProductService(_repository, _unitOfWork, new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task CreateAsync_PersistsProductAndInitialMovement()
    {
        var response = await _sut.CreateAsync(new CreateProductRequest { Name = "Keyboard", Description = null, Price = 50m, InitialStock = 10 }, "tester", CancellationToken.None);

        response.Name.ShouldBe("Keyboard");
        response.Stock.ShouldBe(10);
        response.CreatedAt.ShouldBe(Now);
        _repository.Received(1).Add(Arg.Is<Product>(p => p.Id == response.Id));
        _repository.Received(1).AddMovement(Arg.Is<StockMovement>(m => m.ProductId == response.Id && m.Quantity == 10));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithZeroStock_DoesNotRecordMovement()
    {
        await _sut.CreateAsync(new CreateProductRequest { Name = "Keyboard", Description = null, Price = 50m, InitialStock = 0 }, "tester", CancellationToken.None);

        _repository.DidNotReceive().AddMovement(Arg.Any<StockMovement>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WithStaleVersion_ThrowsPreconditionFailed()
    {
        var product = Product.Create("Keyboard", null, 50m, 1, "tester", Now);
        _repository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await Should.ThrowAsync<PreconditionFailedException>(
            () => _sut.UpdateAsync(product.Id, new UpdateProductRequest { Name = "Mouse", Description = null, Price = 10m }, expectedVersion: 99, "tester", CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None));

        _repository.DidNotReceive().Remove(Arg.Any<Product>());
    }

    [Fact]
    public async Task AdjustStockAsync_Decrease_UpdatesStockAndRecordsMovement()
    {
        var product = Product.Create("Keyboard", null, 50m, 10, "tester", Now);
        _repository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.AdjustStockAsync(product.Id, new AdjustStockRequest { Operation = StockOperation.Decrease, Quantity = 4, Reason = "Sale" }, null, "tester", CancellationToken.None);

        result.IsReplay.ShouldBeFalse();
        result.Response.PreviousStock.ShouldBe(10);
        result.Response.CurrentStock.ShouldBe(6);
        result.Response.Operation.ShouldBe(StockOperation.Decrease);
        _repository.Received(1).AddMovement(Arg.Is<StockMovement>(m => m.Quantity == -4));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdjustStockAsync_BeyondAvailable_ThrowsAndDoesNotSave()
    {
        var product = Product.Create("Keyboard", null, 50m, 2, "tester", Now);
        _repository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await Should.ThrowAsync<InsufficientStockException>(
            () => _sut.AdjustStockAsync(product.Id, new AdjustStockRequest { Operation = StockOperation.Decrease, Quantity = 3, Reason = null }, null, "tester", CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdjustStockAsync_WhenProductMissing_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(
            () => _sut.AdjustStockAsync(Guid.NewGuid(), new AdjustStockRequest { Operation = StockOperation.Increase, Quantity = 1, Reason = null }, null, "tester", CancellationToken.None));
    }

    [Fact]
    public async Task AdjustStockAsync_WithUsedIdempotencyKey_ReplaysWithoutChangingStock()
    {
        var product = Product.Create("Keyboard", null, 50m, 10, "tester", Now);
        var previous = product.DecreaseStock(3, null, "key-1", "tester", Now);
        _repository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _repository.FindMovementByIdempotencyKeyAsync("key-1", Arg.Any<CancellationToken>()).Returns(previous);

        var result = await _sut.AdjustStockAsync(product.Id, new AdjustStockRequest { Operation = StockOperation.Decrease, Quantity = 3, Reason = null }, "key-1", "tester", CancellationToken.None);

        result.IsReplay.ShouldBeTrue();
        result.Response.MovementId.ShouldBe(previous.Id);
        result.Response.PreviousStock.ShouldBe(10);
        result.Response.CurrentStock.ShouldBe(7);
        product.Stock.ShouldBe(7);
        _repository.DidNotReceive().AddMovement(Arg.Any<StockMovement>());
    }

    [Fact]
    public async Task AdjustStockAsync_WithIdempotencyKeyReusedForAnotherPayload_Throws()
    {
        var product = Product.Create("Keyboard", null, 50m, 10, "tester", Now);
        var previous = product.DecreaseStock(3, null, "key-1", "tester", Now);
        _repository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _repository.FindMovementByIdempotencyKeyAsync("key-1", Arg.Any<CancellationToken>()).Returns(previous);

        var exception = await Should.ThrowAsync<UnprocessableRequestException>(
            () => _sut.AdjustStockAsync(product.Id, new AdjustStockRequest { Operation = StockOperation.Increase, Quantity = 3, Reason = null }, "key-1", "tester", CancellationToken.None));

        exception.Code.ShouldBe("idempotency_key_reused");
    }
}
