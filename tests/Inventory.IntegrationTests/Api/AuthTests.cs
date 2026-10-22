using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inventory.IntegrationTests.Persistence;

namespace Inventory.IntegrationTests.Api;

/// <summary>
/// Pruebas de inicio de sesión, protección de endpoints y límite de intentos
/// contra los usuarios del seed.
/// </summary>
[Collection(DatabaseTestGroup.Name)]
public sealed class AuthTests(DatabaseFixture database) : IDisposable
{
    private static readonly Uri LoginUri = new("/api/auth/login", UriKind.Relative);
    private static readonly Uri MeUri = new("/api/auth/me", UriKind.Relative);

    private readonly ApiFactory factory = new(database.ConnectionString);

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenThatAuthenticatesRequests()
    {
        HttpClient client = factory.CreateClient();

        using HttpResponseMessage login = await client.PostAsJsonAsync(LoginUri, new { username = "admin", password = "Admin123*" });
        using JsonDocument loginBody = await ReadJsonAsync(login);
        string? token = loginBody.RootElement.GetProperty("accessToken").GetString();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("Bearer", loginBody.RootElement.GetProperty("tokenType").GetString());
        Assert.Equal("Admin", loginBody.RootElement.GetProperty("user").GetProperty("role").GetString());

        using HttpRequestMessage request = new(HttpMethod.Get, MeUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage me = await client.SendAsync(request);
        using JsonDocument meBody = await ReadJsonAsync(me);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal("admin", meBody.RootElement.GetProperty("username").GetString());
        Assert.Equal(database.AdminUserId, meBody.RootElement.GetProperty("id").GetInt64());
    }

    [Theory]
    [InlineData("admin", "Incorrecta1*")]
    [InlineData("no-existe", "Admin123*")]
    public async Task Login_InvalidCredentials_Returns401WithSameMessage(string username, string password)
    {
        using HttpResponseMessage response = await factory.CreateClient().PostAsJsonAsync(LoginUri, new { username, password });
        using JsonDocument body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("invalid_credentials", body.RootElement.GetProperty("code").GetString());
        Assert.Equal("Usuario o contraseña incorrectos.", body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Login_MissingFields_Returns400()
    {
        using HttpResponseMessage response = await factory.CreateClient().PostAsJsonAsync(LoginUri, new { username = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        using HttpResponseMessage response = await factory.CreateClient().GetAsync(MeUri);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTamperedToken_Returns401()
    {
        HttpClient client = factory.CreateClient();
        using HttpResponseMessage login = await client.PostAsJsonAsync(LoginUri, new { username = "operador", password = "Operador123*" });
        using JsonDocument loginBody = await ReadJsonAsync(login);
        string token = loginBody.RootElement.GetProperty("accessToken").GetString()!;

        using HttpRequestMessage request = new(HttpMethod.Get, MeUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token[..^4] + "AAAA");
        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_TooManyAttempts_Returns429()
    {
        HttpClient client = factory.CreateClient();
        List<HttpStatusCode> statuses = new();

        for (int attempt = 0; attempt < 6; attempt++)
        {
            using HttpResponseMessage response = await client.PostAsJsonAsync(LoginUri, new { username = "admin", password = "Mala" });
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(5), status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }

    public void Dispose() => factory.Dispose();

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
}
