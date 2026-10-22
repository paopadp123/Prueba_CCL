using System.Net;
using System.Text.Json;
using Inventory.Api.Observability;
using Inventory.IntegrationTests.Persistence;
using Npgsql;

namespace Inventory.IntegrationTests.Api;

/// <summary>
/// Pruebas del pipeline HTTP: errores, correlación, health check y OpenAPI.
/// </summary>
[Collection(DatabaseTestGroup.Name)]
public sealed class ApiPipelineTests(DatabaseFixture database) : IDisposable
{
    private const string ProblemJson = "application/problem+json";

    private readonly ApiFactory factory = new(database.ConnectionString);

    [Fact]
    public async Task DomainError_ReturnsProblemDetailsWithCodeAndValues()
    {
        using HttpResponseMessage response = await factory.CreateClient().GetAsync(new Uri("/test/errors/stock", UriKind.Relative));
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("insufficient_stock", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(2, body.RootElement.GetProperty("available").GetInt32());
        Assert.Equal(8, body.RootElement.GetProperty("requested").GetInt32());
        Assert.Equal("/test/errors/stock", body.RootElement.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task UnexpectedError_Returns500WithoutInternalDetails()
    {
        using HttpResponseMessage response = await factory.CreateClient().GetAsync(new Uri("/test/errors/boom", UriKind.Relative));
        string text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("unexpected_error", text, StringComparison.Ordinal);
        Assert.DoesNotContain("detalle interno", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_EchoesIncomingCorrelationIdInHeaderAndBody()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri("/test/errors/not-found", UriKind.Relative));
        request.Headers.Add(CorrelationId.HeaderName, "prueba-123");

        using HttpResponseMessage response = await factory.CreateClient().SendAsync(request);
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("prueba-123", response.Headers.GetValues(CorrelationId.HeaderName).Single());
        Assert.Equal("prueba-123", body.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetailsWithGeneratedCorrelationId()
    {
        using HttpResponseMessage response = await factory.CreateClient().GetAsync(new Uri("/no-existe", UriKind.Relative));
        using JsonDocument body = await ReadJsonAsync(response);

        string correlationId = response.Headers.GetValues(CorrelationId.HeaderName).Single();
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(correlationId, body.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Health_WithDatabaseAvailable_ReturnsHealthy()
    {
        using HttpResponseMessage response = await factory.CreateClient().GetAsync(new Uri("/health", UriKind.Relative));
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        JsonElement check = Assert.Single(body.RootElement.GetProperty("checks").EnumerateArray());
        Assert.Equal("database", check.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Health_WithDatabaseUnavailable_Returns503()
    {
        NpgsqlConnectionStringBuilder unreachable = new(database.ConnectionString) { Database = "no_existe_inventory" };
        using ApiFactory brokenFactory = new(unreachable.ConnectionString);

        using HttpResponseMessage response = await brokenFactory.CreateClient().GetAsync(new Uri("/health", UriKind.Relative));
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task OpenApi_DeclaresBearerSecurityScheme()
    {
        using HttpResponseMessage response = await factory.CreateClient().GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        using JsonDocument body = await ReadJsonAsync(response);

        JsonElement scheme = body.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
    }

    public void Dispose() => factory.Dispose();

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
}
