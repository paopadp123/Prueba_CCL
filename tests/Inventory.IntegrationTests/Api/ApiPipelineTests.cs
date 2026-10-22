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
        using var response = await factory.CreateClient().GetAsync(new Uri("/test/errors/stock", UriKind.Relative));
        using var body = await ReadJsonAsync(response);

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
        using var response = await factory.CreateClient().GetAsync(new Uri("/test/errors/boom", UriKind.Relative));
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("unexpected_error", text, StringComparison.Ordinal);
        Assert.DoesNotContain("detalle interno", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_EchoesIncomingCorrelationIdInHeaderAndBody()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/test/errors/not-found", UriKind.Relative));
        request.Headers.Add(CorrelationId.HeaderName, "prueba-123");

        using var response = await factory.CreateClient().SendAsync(request);
        using var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("prueba-123", response.Headers.GetValues(CorrelationId.HeaderName).Single());
        Assert.Equal("prueba-123", body.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetailsWithGeneratedCorrelationId()
    {
        using var response = await factory.CreateClient().GetAsync(new Uri("/no-existe", UriKind.Relative));
        using var body = await ReadJsonAsync(response);

        var correlationId = response.Headers.GetValues(CorrelationId.HeaderName).Single();
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(correlationId, body.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Health_WithDatabaseAvailable_ReturnsHealthy()
    {
        using var response = await factory.CreateClient().GetAsync(new Uri("/health", UriKind.Relative));
        using var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        var check = Assert.Single(body.RootElement.GetProperty("checks").EnumerateArray());
        Assert.Equal("database", check.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Health_WithDatabaseUnavailable_Returns503()
    {
        var unreachable = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = "no_existe_inventory" };
        using var brokenFactory = new ApiFactory(unreachable.ConnectionString);

        using var response = await brokenFactory.CreateClient().GetAsync(new Uri("/health", UriKind.Relative));
        using var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task OpenApi_DeclaresBearerSecurityScheme()
    {
        using var response = await factory.CreateClient().GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        using var body = await ReadJsonAsync(response);

        var scheme = body.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
    }

    public void Dispose() => factory.Dispose();

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
}
