using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inventory.IntegrationTests.Persistence;

namespace Inventory.IntegrationTests.Api;

/// <summary>
/// Pruebas de los endpoints de productos: permisos por rol, validación y errores de negocio.
/// Cada prueba usa SKUs propios para no depender de las demás.
/// </summary>
[Collection(DatabaseTestGroup.Name)]
public sealed class ProductsTests(DatabaseFixture database) : IDisposable
{
    private static readonly Uri ProductsUri = new("/api/products", UriKind.Relative);

    private readonly ApiFactory factory = new(database.ConnectionString);

    [Fact]
    public async Task Create_AsAdmin_Returns201WithLocationAndProductCanBeRead()
    {
        HttpClient admin = await AdminAsync();

        using HttpResponseMessage created = await admin.PostAsJsonAsync(ProductsUri, NewProduct("api-crt-001"));
        using JsonDocument body = await ReadJsonAsync(created);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("API-CRT-001", body.RootElement.GetProperty("sku").GetString());
        Assert.Equal(0, body.RootElement.GetProperty("currentStock").GetInt32());

        using HttpResponseMessage read = await admin.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    [Fact]
    public async Task Create_RecordsTokenUserInAudit()
    {
        HttpClient admin = await AdminAsync();
        long id = await CreateAsync(admin, "API-AUD-001");

        long auditUser = await database.ScalarAsync<long>(
            "SELECT app_user_id FROM inventory.audit_log WHERE table_name = 'products' AND record_id = @id AND operation = 'INSERT'",
            ("id", id));

        Assert.Equal(database.AdminUserId, auditUser);
    }

    [Fact]
    public async Task Create_DuplicateSku_Returns409()
    {
        HttpClient admin = await AdminAsync();
        using HttpResponseMessage first = await admin.PostAsJsonAsync(ProductsUri, NewProduct("API-DUP-001"));

        using HttpResponseMessage second = await admin.PostAsJsonAsync(ProductsUri, NewProduct("API-DUP-001"));
        using JsonDocument body = await ReadJsonAsync(second);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("duplicate_sku", body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Create_InvalidSku_Returns400WithCode()
    {
        HttpClient admin = await AdminAsync();

        using HttpResponseMessage response = await admin.PostAsJsonAsync(ProductsUri, NewProduct("-mal formato"));
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_sku", body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400WithFieldErrors()
    {
        HttpClient admin = await AdminAsync();

        using HttpResponseMessage response = await admin.PostAsJsonAsync(ProductsUri, new { sku = "API-REQ-001" });
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement errors = body.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Name", out _));
        Assert.True(errors.TryGetProperty("UnitPrice", out _));
    }

    [Fact]
    public async Task Create_AsOperator_Returns403()
    {
        HttpClient operatorClient = await OperatorAsync();

        using HttpResponseMessage response = await operatorClient.PostAsJsonAsync(ProductsUri, NewProduct("API-OPE-001"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_AsOperator_FiltersAndPaginates()
    {
        HttpClient admin = await AdminAsync();
        foreach (string sku in new[] { "API-LST-001", "API-LST-002", "API-LST-003" })
        {
            using HttpResponseMessage created = await admin.PostAsJsonAsync(ProductsUri, NewProduct(sku));
        }

        HttpClient operatorClient = await OperatorAsync();
        using HttpResponseMessage response = await operatorClient.GetAsync(
            new Uri("/api/products?search=api-lst&page=2&pageSize=2", UriKind.Relative));
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, body.RootElement.GetProperty("totalCount").GetInt64());
        Assert.Equal(2, body.RootElement.GetProperty("totalPages").GetInt32());
        Assert.Single(body.RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Update_AsAdmin_ChangesDetails()
    {
        HttpClient admin = await AdminAsync();
        long id = await CreateAsync(admin, "API-UPD-001");

        using HttpResponseMessage response = await admin.PutAsJsonAsync(
            ProductUri(id), new { name = "Nombre editado", description = "Nueva", unitPrice = 250.75m });
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Nombre editado", body.RootElement.GetProperty("name").GetString());
        Assert.Equal(250.75m, body.RootElement.GetProperty("unitPrice").GetDecimal());
        Assert.Equal("API-UPD-001", body.RootElement.GetProperty("sku").GetString());
    }

    [Fact]
    public async Task ChangeStatus_Deactivate_HidesProductFromDefaultList()
    {
        HttpClient admin = await AdminAsync();
        long id = await CreateAsync(admin, "API-STS-001");

        using HttpResponseMessage response = await admin.PatchAsJsonAsync(
            new Uri($"/api/products/{id}/status", UriKind.Relative), new { isActive = false });
        using HttpResponseMessage activeOnly = await admin.GetAsync(new Uri("/api/products?search=API-STS-001", UriKind.Relative));
        using HttpResponseMessage withInactive = await admin.GetAsync(
            new Uri("/api/products?search=API-STS-001&includeInactive=true", UriKind.Relative));
        using JsonDocument activeBody = await ReadJsonAsync(activeOnly);
        using JsonDocument allBody = await ReadJsonAsync(withInactive);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, activeBody.RootElement.GetProperty("totalCount").GetInt64());
        Assert.Equal(1, allBody.RootElement.GetProperty("totalCount").GetInt64());
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        HttpClient operatorClient = await OperatorAsync();

        using HttpResponseMessage response = await operatorClient.GetAsync(ProductUri(999_999));
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("product_not_found", body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        using HttpResponseMessage response = await factory.CreateClient().GetAsync(ProductsUri);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose() => factory.Dispose();

    private Task<HttpClient> AdminAsync() => factory.CreateAuthenticatedClientAsync("admin", "Admin123*");

    private Task<HttpClient> OperatorAsync() => factory.CreateAuthenticatedClientAsync("operador", "Operador123*");

    private static object NewProduct(string sku) =>
        new { sku, name = $"Producto {sku}", description = (string?)null, unitPrice = 1000m };

    private static Uri ProductUri(long id) => new($"/api/products/{id}", UriKind.Relative);

    private static async Task<long> CreateAsync(HttpClient client, string sku)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(ProductsUri, NewProduct(sku));
        using JsonDocument body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("id").GetInt64();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
}
