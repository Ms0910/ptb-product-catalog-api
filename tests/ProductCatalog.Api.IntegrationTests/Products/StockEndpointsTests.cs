using System.Net;
using System.Net.Http.Json;
using ProductCatalog.Api.IntegrationTests.Infrastructure;
using ProductCatalog.Application.Common.Pagination;
using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Api.IntegrationTests.Products;

[Collection(ApiTestGroup.Name)]
public sealed class StockEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task IncreaseAndDecrease_UpdateStock()
    {
        var product = await _client.CreateProductAsync(initialStock: 10);

        var increase = await _client.AdjustStockAsync(product.Id, "Increase", 5);
        var decrease = await _client.AdjustStockAsync(product.Id, "Decrease", 12);

        increase.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await increase.ReadAsync<StockAdjustmentResponse>()).CurrentStock.ShouldBe(15);

        var result = await decrease.ReadAsync<StockAdjustmentResponse>();
        result.PreviousStock.ShouldBe(15);
        result.CurrentStock.ShouldBe(3);
        result.Operation.ShouldBe(StockOperation.Decrease);
        (await GetStockAsync(product.Id)).ShouldBe(3);
    }

    [Fact]
    public async Task Decrease_BeyondAvailable_ReturnsConflictAndKeepsStock()
    {
        var product = await _client.CreateProductAsync(initialStock: 2);

        var response = await _client.AdjustStockAsync(product.Id, "Decrease", 3);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.ReadJsonAsync();
        problem.GetProperty("code").GetString().ShouldBe("product.insufficient_stock");
        problem.GetProperty("availableStock").GetInt32().ShouldBe(2);
        problem.GetProperty("requestedQuantity").GetInt32().ShouldBe(3);
        (await GetStockAsync(product.Id)).ShouldBe(2);
    }

    [Theory]
    [InlineData("Decrease", 0)]
    [InlineData("Increase", -3)]
    [InlineData("Remove", 1)]
    public async Task InvalidRequest_ReturnsBadRequest(string operation, int quantity)
    {
        var product = await _client.CreateProductAsync();

        var response = await _client.AdjustStockAsync(product.Id, operation, quantity);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnknownProduct_ReturnsNotFound()
    {
        var response = await _client.AdjustStockAsync(Guid.NewGuid(), "Increase", 1);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SameIdempotencyKey_IsAppliedOnce_AndReplaysTheOriginalResult()
    {
        var product = await _client.CreateProductAsync(initialStock: 10);
        var key = Guid.NewGuid().ToString();

        var first = await _client.AdjustStockAsync(product.Id, "Decrease", 4, key);
        var retry = await _client.AdjustStockAsync(product.Id, "Decrease", 4, key);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
        retry.Headers.GetValues("Idempotent-Replayed").ShouldBe(["true"]);
        (await retry.ReadAsync<StockAdjustmentResponse>()).ShouldBe(await first.ReadAsync<StockAdjustmentResponse>());
        (await GetStockAsync(product.Id)).ShouldBe(6);
    }

    [Fact]
    public async Task IdempotencyKey_ReusedWithDifferentPayload_ReturnsUnprocessable()
    {
        var product = await _client.CreateProductAsync(initialStock: 10);
        var key = Guid.NewGuid().ToString();

        await _client.AdjustStockAsync(product.Id, "Decrease", 1, key);
        var response = await _client.AdjustStockAsync(product.Id, "Decrease", 2, key);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("idempotency_key_reused");
    }

    [Fact]
    public async Task StockMovements_ListTheHistoryNewestFirst()
    {
        var product = await _client.CreateProductAsync(initialStock: 5);
        await _client.AdjustStockAsync(product.Id, "Increase", 2);
        await _client.AdjustStockAsync(product.Id, "Decrease", 4);

        var history = await _client.GetFromJsonAsync<PagedResult<StockMovementResponse>>(
            $"/api/products/{product.Id}/stock-movements", ApiClient.Json);

        history!.TotalCount.ShouldBe(3);
        history.Items.Select(m => (m.Operation, m.Quantity, m.ResultingStock)).ShouldBe(
        [
            (StockOperation.Decrease, 4, 3),
            (StockOperation.Increase, 2, 7),
            (StockOperation.Increase, 5, 5),
        ]);
    }

    [Fact]
    public async Task ConcurrentDecreases_NeverOversell()
    {
        const int initialStock = 10;
        const int concurrentRequests = 50;
        var product = await _client.CreateProductAsync(initialStock: initialStock);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, concurrentRequests).Select(_ => _client.AdjustStockAsync(product.Id, "Decrease", 1)));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(initialStock);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(concurrentRequests - initialStock);
        (await GetStockAsync(product.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task ConcurrentIncreasesAndDecreases_LoseNoUpdates()
    {
        var product = await _client.CreateProductAsync(initialStock: 100);

        var requests = Enumerable.Range(0, 60).Select(i => _client.AdjustStockAsync(product.Id, i % 2 == 0 ? "Increase" : "Decrease", 3));
        var responses = await Task.WhenAll(requests);

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.OK);
        (await GetStockAsync(product.Id)).ShouldBe(100);

        var history = await _client.GetFromJsonAsync<PagedResult<StockMovementResponse>>(
            $"/api/products/{product.Id}/stock-movements?pageSize=100", ApiClient.Json);
        history!.TotalCount.ShouldBe(61);
    }

    [Fact]
    public async Task ConcurrentRetriesWithSameIdempotencyKey_ApplyOnce()
    {
        var product = await _client.CreateProductAsync(initialStock: 20);
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => _client.AdjustStockAsync(product.Id, "Decrease", 5, key)));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.OK);
        responses.Count(r => r.Headers.Contains("Idempotent-Replayed")).ShouldBe(9);
        (await GetStockAsync(product.Id)).ShouldBe(15);
    }

    private async Task<int> GetStockAsync(Guid productId) =>
        (await _client.GetFromJsonAsync<ProductResponse>($"/api/products/{productId}", ApiClient.Json))!.Stock;
}
