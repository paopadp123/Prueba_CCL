using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inventory.IntegrationTests.Persistence;

namespace Inventory.IntegrationTests.Api;

/// <summary>
/// Pruebas de los endpoints de movimientos contra los procedimientos almacenados reales,
/// incluida la concurrencia de salidas sobre el mismo producto.
/// </summary>
[Collection(DatabaseTestGroup.Name)]
public sealed class MovementsTests(DatabaseFixture database) : IDisposable
{
    private static readonly Uri EntriesUri = new("/api/movements/entries", UriKind.Relative);
    private static readonly Uri ExitsUri = new("/api/movements/exits", UriKind.Relative);

    private readonly ApiFactory factory = new(database.ConnectionString);

    [Fact]
    public async Task EntryAndExit_AsOperator_UpdateStockAndRecordUser()
    {
        long productId = await CreateProductAsync("MOV-API-001");
        HttpClient operatorClient = await OperatorAsync();

        using HttpResponseMessage entry = await operatorClient.PostAsJsonAsync(
            EntriesUri, new { productId, quantity = 10, reason = "Compra" });
        using JsonDocument entryBody = await ReadJsonAsync(entry);
        using HttpResponseMessage exit = await operatorClient.PostAsJsonAsync(
            ExitsUri, new { productId, quantity = 4, reason = "Venta" });
        using JsonDocument exitBody = await ReadJsonAsync(exit);

        Assert.Equal(HttpStatusCode.Created, entry.StatusCode);
        Assert.Equal("Entry", entryBody.RootElement.GetProperty("type").GetString());
        Assert.Equal(10, entryBody.RootElement.GetProperty("stockAfter").GetInt32());
        Assert.Equal(HttpStatusCode.Created, exit.StatusCode);
        Assert.Equal(6, exitBody.RootElement.GetProperty("stockAfter").GetInt32());

        using HttpResponseMessage history = await operatorClient.GetAsync(exit.Headers.Location);
        using JsonDocument historyBody = await ReadJsonAsync(history);
        JsonElement latest = historyBody.RootElement.GetProperty("items")[0];
        Assert.Equal(2, historyBody.RootElement.GetProperty("totalCount").GetInt64());
        Assert.Equal("Exit", latest.GetProperty("type").GetString());
        Assert.Equal("operador", latest.GetProperty("username").GetString());
    }

    [Fact]
    public async Task Exit_AboveStock_Returns409WithAvailableAndRequested()
    {
        long productId = await CreateProductAsync("MOV-API-002");
        HttpClient admin = await AdminAsync();
        using HttpResponseMessage entry = await admin.PostAsJsonAsync(EntriesUri, new { productId, quantity = 3 });

        using HttpResponseMessage exit = await admin.PostAsJsonAsync(ExitsUri, new { productId, quantity = 5 });
        using JsonDocument body = await ReadJsonAsync(exit);

        Assert.Equal(HttpStatusCode.Conflict, exit.StatusCode);
        Assert.Equal("insufficient_stock", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(3, body.RootElement.GetProperty("available").GetInt32());
        Assert.Equal(5, body.RootElement.GetProperty("requested").GetInt32());
    }

    [Fact]
    public async Task Exit_InactiveProduct_Returns409()
    {
        long productId = await CreateProductAsync("MOV-API-003");
        HttpClient admin = await AdminAsync();
        using HttpResponseMessage deactivated = await admin.PatchAsJsonAsync(
            new Uri($"/api/products/{productId}/status", UriKind.Relative), new { isActive = false });

        using HttpResponseMessage entry = await admin.PostAsJsonAsync(EntriesUri, new { productId, quantity = 1 });
        using JsonDocument body = await ReadJsonAsync(entry);

        Assert.Equal(HttpStatusCode.Conflict, entry.StatusCode);
        Assert.Equal("inactive_product", body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Entry_UnknownProduct_Returns404()
    {
        HttpClient admin = await AdminAsync();

        using HttpResponseMessage response = await admin.PostAsJsonAsync(EntriesUri, new { productId = 999_999, quantity = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Entry_NonPositiveQuantity_Returns400(int quantity)
    {
        HttpClient admin = await AdminAsync();

        using HttpResponseMessage response = await admin.PostAsJsonAsync(EntriesUri, new { productId = 1, quantity });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task History_UnknownProduct_Returns404()
    {
        HttpClient admin = await AdminAsync();

        using HttpResponseMessage response = await admin.GetAsync(new Uri("/api/products/999999/movements", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentExits_NeverOversellStock()
    {
        long productId = await CreateProductAsync("MOV-API-CON");
        HttpClient admin = await AdminAsync();
        using HttpResponseMessage entry = await admin.PostAsJsonAsync(EntriesUri, new { productId, quantity = 5 });

        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => admin.PostAsJsonAsync(ExitsUri, new { productId, quantity = 1 })));

        int created = responses.Count(response => response.StatusCode == HttpStatusCode.Created);
        int conflicts = responses.Count(response => response.StatusCode == HttpStatusCode.Conflict);
        foreach (HttpResponseMessage response in responses)
        {
            response.Dispose();
        }

        int finalStock = await database.ScalarAsync<int>(
            "SELECT current_stock FROM inventory.products WHERE id = @id", ("id", productId));

        Assert.Equal(5, created);
        Assert.Equal(5, conflicts);
        Assert.Equal(0, finalStock);
    }

    public void Dispose() => factory.Dispose();

    private Task<HttpClient> AdminAsync() => factory.CreateAuthenticatedClientAsync("admin", "Admin123*");

    private Task<HttpClient> OperatorAsync() => factory.CreateAuthenticatedClientAsync("operador", "Operador123*");

    private async Task<long> CreateProductAsync(string sku)
    {
        HttpClient admin = await AdminAsync();
        using HttpResponseMessage response = await admin.PostAsJsonAsync(
            new Uri("/api/products", UriKind.Relative), new { sku, name = $"Producto {sku}", unitPrice = 100m });
        using JsonDocument body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("id").GetInt64();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
}
