using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProductCatalog.Application.Products.Contracts;

namespace ProductCatalog.Api.IntegrationTests.Infrastructure;

internal static class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<ProductResponse> CreateProductAsync(this HttpClient client, string name = "Keyboard", decimal price = 99.90m, int initialStock = 10, string? actor = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/products")
        {
            Content = JsonContent.Create(new { name, description = "Test product", price, initialStock }),
        };

        if (actor is not null)
        {
            request.Headers.Add("X-Actor", actor);
        }

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>(Json))!;
    }

    public static Task<HttpResponseMessage> AdjustStockAsync(this HttpClient client, Guid productId, string operation, int quantity, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/products/{productId}/stock")
        {
            Content = JsonContent.Create(new { operation, quantity, reason = "test" }),
        };

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return client.SendAsync(request);
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Json);
}
