using Inventory.Application.Models;
using Inventory.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Inventory.UnitTests.Infrastructure;

/// <summary>
/// Pruebas de BCrypt y de la emisión de tokens JWT.
/// </summary>
public class SecurityTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Settings = new()
    {
        Issuer = "Inventory.Api",
        Audience = "Inventory.Client",
        SigningKey = "clave-de-pruebas-unitarias-0123456789",
        ExpirationMinutes = 30,
    };

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        string hash = BCryptNet.HashPassword("Admin123*", workFactor: 4);

        Assert.True(new BCryptPasswordHasher().Verify("Admin123*", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        string hash = BCryptNet.HashPassword("Admin123*", workFactor: 4);

        Assert.False(new BCryptPasswordHasher().Verify("admin123*", hash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-es-un-hash")]
    public void Verify_MissingOrMalformedHash_ReturnsFalse(string? hash)
    {
        Assert.False(new BCryptPasswordHasher().Verify("Admin123*", hash));
    }

    [Fact]
    public async Task CreateToken_ContainsUserClaimsAndExpiration()
    {
        JwtTokenService service = new(Options.Create(Settings), new FixedTimeProvider(Now));

        AccessToken accessToken = service.CreateToken(new AuthenticatedUser(7, "operador", "Operador de bodega", "Operator"));
        TokenValidationResult validation = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken.Token, new TokenValidationParameters
        {
            ValidIssuer = Settings.Issuer,
            ValidAudience = Settings.Audience,
            IssuerSigningKey = Settings.CreateSigningKey(),
            ValidateLifetime = false,
        });

        Assert.True(validation.IsValid);
        Assert.Equal(Now.AddMinutes(30), accessToken.ExpiresAt);
        Assert.Equal("7", validation.Claims[JwtClaimNames.Subject]);
        Assert.Equal("operador", validation.Claims[JwtClaimNames.Username]);
        Assert.Equal("Operator", validation.Claims[JwtClaimNames.Role]);
    }

    [Fact]
    public async Task CreateToken_SignedWithOtherKey_IsRejected()
    {
        JwtTokenService service = new(Options.Create(Settings), new FixedTimeProvider(Now));

        AccessToken accessToken = service.CreateToken(new AuthenticatedUser(7, "operador", "Operador", "Operator"));
        TokenValidationResult validation = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken.Token, new TokenValidationParameters
        {
            ValidIssuer = Settings.Issuer,
            ValidAudience = Settings.Audience,
            IssuerSigningKey = new JwtOptions { SigningKey = "otra-clave-distinta-de-al-menos-32-caracteres" }.CreateSigningKey(),
            ValidateLifetime = false,
        });

        Assert.False(validation.IsValid);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
