using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inventory.IntegrationTests.Persistence;

namespace Inventory.IntegrationTests.Api;

/// <summary>
/// Pruebas de la consulta de stock contra fn_get_stock y la tabla de productos.
/// </summary>
[Collection(DatabaseTestGroup.Name)]
public sealed class StockTests(DatabaseFixture database) : IDisposable
{
    private readonly ApiFactory factory = new(database.ConnectionString);

    [Fact]
    public async Task GetByProduct_AfterMovements_ReturnsStockAndValue()
    {
        HttpClient admin = await AdminAsync();
        long productId = await CreateProductAsync(admin, "STK-API-001", 12.5m);
        await RegisterAsync(admin, "entries", productId, 10);
        await RegisterAsync(admin, "exits", productId, 3);
        HttpClient operatorClient = await factory.CreateAuthenticatedClientAsync("operador", "Operador123*");

        using HttpResponseMessage response = await operatorClient.GetAsync(StockOf(productId));
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("STK-API-001", body.RootElement.GetProperty("sku").GetString());
        Assert.Equal(7, body.RootElement.GetProperty("currentStock").GetInt32());
        Assert.Equal(87.5m, body.RootElement.GetProperty("stockValue").GetDecimal());
    }

    [Fact]
    public async Task GetByProduct_InactiveProduct_StillReturnsStock()
    {
        HttpClient admin = await AdminAsync();
        long productId = await CreateProductAsync(admin, "STK-API-002", 10m);
        await RegisterAsync(admin, "entries", productId, 2);
        using HttpResponseMessage deactivated = await DeactivateAsync(admin, productId);

        using HttpResponseMessage response = await admin.GetAsync(StockOf(productId));
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, body.RootElement.GetProperty("currentStock").GetInt32());
        Assert.False(body.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task GetByProduct_UnknownProduct_Returns404()
    {
        HttpClient admin = await AdminAsync();

        using HttpResponseMessage response = await admin.GetAsync(StockOf(999_999));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_FiltersBySearchAndExcludesInactiveByDefault()
    {
        HttpClient admin = await AdminAsync();
        await CreateProductAsync(admin, "STK-LST-001", 1m);
        long inactiveId = await CreateProductAsync(admin, "STK-LST-002", 1m);
        await CreateProductAsync(admin, "OTRO-LST-003", 1m);
        using HttpResponseMessage deactivated = await DeactivateAsync(admin, inactiveId);

        using JsonDocument active = await ListAsync(admin, "?search=stk-lst");
        using JsonDocument all = await ListAsync(admin, "?search=stk-lst&includeInactive=true");

        Assert.Equal(1, active.RootElement.GetProperty("totalCount").GetInt64());
        Assert.Equal("STK-LST-001", active.RootElement.GetProperty("items")[0].GetProperty("sku").GetString());
        Assert.Equal(2, all.RootElement.GetProperty("totalCount").GetInt64());
    }

    [Fact]
    public async Task List_Paginates()
    {
        HttpClient admin = await AdminAsync();
        await CreateProductAsync(admin, "STK-PAG-001", 1m);
        await CreateProductAsync(admin, "STK-PAG-002", 1m);
        await CreateProductAsync(admin, "STK-PAG-003", 1m);

        using JsonDocument body = await ListAsync(admin, "?search=stk-pag&page=2&pageSize=2");

        Assert.Equal(3, body.RootElement.GetProperty("totalCount").GetInt64());
        Assert.Equal(2, body.RootElement.GetProperty("totalPages").GetInt32());
        JsonElement item = Assert.Single(body.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("STK-PAG-003", item.GetProperty("sku").GetString());
    }

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        using HttpClient anonymous = factory.CreateClient();

        using HttpResponseMessage response = await anonymous.GetAsync(new Uri("/api/stock", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose() => factory.Dispose();

    private static Uri StockOf(long productId) => new($"/api/products/{productId}/stock", UriKind.Relative);

    private Task<HttpClient> AdminAsync() => factory.CreateAuthenticatedClientAsync("admin", "Admin123*");

    private static async Task<long> CreateProductAsync(HttpClient admin, string sku, decimal unitPrice)
    {
        using HttpResponseMessage response = await admin.PostAsJsonAsync(
            new Uri("/api/products", UriKind.Relative), new { sku, name = $"Producto {sku}", unitPrice });
        using JsonDocument body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("id").GetInt64();
    }

    private static Task<HttpResponseMessage> DeactivateAsync(HttpClient admin, long productId) =>
        admin.PatchAsJsonAsync(new Uri($"/api/products/{productId}/status", UriKind.Relative), new { isActive = false });

    private static async Task RegisterAsync(HttpClient client, string kind, long productId, int quantity)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/movements/{kind}", UriKind.Relative), new { productId, quantity });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<JsonDocument> ListAsync(HttpClient client, string query)
    {
        using HttpResponseMessage response = await client.GetAsync(new Uri($"/api/stock{query}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadJsonAsync(response);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
}
