using Inventory.Application.Abstractions;
using Inventory.Application.Auth;
using Inventory.Application.Models;

namespace Inventory.UnitTests.Application;

/// <summary>
/// Pruebas del caso de uso de inicio de sesión.
/// </summary>
public class LoginServiceTests
{
    private const string Password = "Secreta123*";
    private static readonly CancellationToken None = CancellationToken.None;

    private readonly FakeUsers users = new();
    private readonly FakeHasher hasher = new();
    private readonly LoginService service;

    public LoginServiceTests()
    {
        service = new LoginService(users, hasher, new FakeTokens());
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUser()
    {
        users.Add(User(isActive: true));

        LoginResult result = await service.LoginAsync("admin", Password, None);

        Assert.Equal("token-for-1", result.AccessToken.Token);
        Assert.Equal(new AuthenticatedUser(1, "admin", "Administrador", "Admin"), result.User);
    }

    [Fact]
    public async Task LoginAsync_TrimsUsername()
    {
        users.Add(User(isActive: true));

        LoginResult result = await service.LoginAsync("  admin ", Password, None);

        Assert.Equal(1, result.User.Id);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentials()
    {
        users.Add(User(isActive: true));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync("admin", "otra", None));
    }

    [Fact]
    public async Task LoginAsync_UnknownUser_StillVerifiesPasswordAndThrows()
    {
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync("nadie", Password, None));

        Assert.Equal(1, hasher.Calls);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsInvalidCredentials()
    {
        users.Add(User(isActive: false));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync("admin", Password, None));
    }

    private static UserCredentials User(bool isActive) =>
        new(1, "admin", $"hash:{Password}", "Administrador", "Admin", isActive);

    private sealed class FakeUsers : IUserRepository
    {
        private readonly Dictionary<string, UserCredentials> byUsername = [];

        public void Add(UserCredentials user) => byUsername[user.Username] = user;

        public Task<UserCredentials?> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(byUsername.GetValueOrDefault(username));
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public int Calls { get; private set; }

        public bool Verify(string password, string? passwordHash)
        {
            Calls++;
            return passwordHash == $"hash:{password}";
        }
    }

    private sealed class FakeTokens : ITokenService
    {
        public AccessToken CreateToken(AuthenticatedUser user) =>
            new($"token-for-{user.Id}", DateTimeOffset.UnixEpoch);
    }
}
