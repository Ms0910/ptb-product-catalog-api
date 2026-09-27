using System.Net;
using System.Net.Http.Json;
using ProductCatalog.Api.IntegrationTests.Infrastructure;
using ProductCatalog.Application.Common.Pagination;
using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Api.IntegrationTests.Products;

[Collection(ApiTestGroup.Name)]
public sealed class ProductEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ReturnsCreatedWithLocationAndETag()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new { name = "Monitor", description = "27 inch", price = 1299.99m, initialStock = 5 });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.ETag.ShouldNotBeNull();

        var product = await response.ReadAsync<ProductResponse>();
        product.Name.ShouldBe("Monitor");
        product.Price.ShouldBe(1299.99m);
        product.Stock.ShouldBe(5);
        response.Headers.Location!.AbsolutePath.ShouldBe($"/api/products/{product.Id}");
    }

    [Fact]
    public async Task Create_WithActorHeader_RoundTripsIntoCreatedBy()
    {
        var product = await _client.CreateProductAsync("Actor test", actor: "qa");

        product.CreatedBy.ShouldBe("qa");
        product.UpdatedBy.ShouldBe("qa");
    }

    [Fact]
    public async Task Create_WithoutActorHeader_DefaultsCreatedByToSystem()
    {
        var product = await _client.CreateProductAsync("No actor test");

        product.CreatedBy.ShouldBe("system");
        product.UpdatedBy.ShouldBe("system");
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new { name = " ", price = -1, initialStock = -5 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");

        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        errors.TryGetProperty("name", out _).ShouldBeTrue();
        errors.TryGetProperty("price", out _).ShouldBeTrue();
        errors.TryGetProperty("initialStock", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Create_WithMissingRequiredFields_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new { name = "Only name" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_ReturnsProduct()
    {
        var created = await _client.CreateProductAsync("Headset");

        var response = await _client.GetAsync($"/api/products/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag!.Tag.ShouldBe($"\"{created.Version}\"");
        (await response.ReadAsync<ProductResponse>()).ShouldBe(created);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFoundProblem()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.ReadJsonAsync();
        problem.GetProperty("code").GetString().ShouldBe("not_found");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task List_ReturnsFilteredSortedPages()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        await _client.CreateProductAsync($"{tag} C", price: 30m);
        await _client.CreateProductAsync($"{tag} A", price: 10m);
        await _client.CreateProductAsync($"{tag} B", price: 20m);

        var firstPage = await _client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            $"/api/products?search={tag}&sortBy=Price&sortDirection=Asc&page=1&pageSize=2", ApiClient.Json);
        var secondPage = await _client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            $"/api/products?search={tag}&sortBy=Price&sortDirection=Asc&page=2&pageSize=2", ApiClient.Json);

        firstPage!.TotalCount.ShouldBe(3);
        firstPage.TotalPages.ShouldBe(2);
        firstPage.Items.Select(p => p.Price).ShouldBe([10m, 20m]);
        secondPage!.Items.Select(p => p.Price).ShouldBe([30m]);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("sortBy=Unknown")]
    public async Task List_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        var response = await _client.GetAsync($"/api/products?{query}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ChangesDetailsButNotStock()
    {
        var created = await _client.CreateProductAsync("Old name", initialStock: 8);

        var response = await _client.PutAsJsonAsync($"/api/products/{created.Id}", new { name = "New name", description = "Updated", price = 55.5m });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.ReadAsync<ProductResponse>();
        updated.Name.ShouldBe("New name");
        updated.Price.ShouldBe(55.5m);
        updated.Stock.ShouldBe(8);
        updated.Version.ShouldNotBe(created.Version);
    }

    [Fact]
    public async Task Update_WithCurrentETag_Succeeds_AndWithStaleETag_ReturnsPreconditionFailed()
    {
        var created = await _client.CreateProductAsync();
        var etag = $"\"{created.Version}\"";

        var first = await SendUpdateAsync(created.Id, etag, "First");
        var second = await SendUpdateAsync(created.Id, etag, "Second");

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        (await _client.GetFromJsonAsync<ProductResponse>($"/api/products/{created.Id}", ApiClient.Json))!.Name.ShouldBe("First");
    }

    [Fact]
    public async Task Update_WithMalformedIfMatch_ReturnsBadRequest()
    {
        var created = await _client.CreateProductAsync();

        var response = await SendUpdateAsync(created.Id, "\"not-a-version\"", "Name");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", new { name = "Name", price = 1m });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_ThenProductIsGone()
    {
        var created = await _client.CreateProductAsync();

        var delete = await _client.DeleteAsync($"/api/products/{created.Id}");
        var get = await _client.GetAsync($"/api/products/{created.Id}");

        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        get.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger/index.html")]
    public async Task OperationalEndpoints_AreAvailable(string url)
    {
        var response = await _client.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private Task<HttpResponseMessage> SendUpdateAsync(Guid id, string ifMatch, string name)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/products/{id}")
        {
            Content = JsonContent.Create(new { name, price = 10m }),
        };
        request.Headers.TryAddWithoutValidation("If-Match", ifMatch);

        return _client.SendAsync(request);
    }
}
