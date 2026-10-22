using System.Globalization;
using Inventory.Application.Abstractions;
using Inventory.Application.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Inventory.Infrastructure.Security;

/// <summary>
/// Emite tokens JWT firmados con HMAC-SHA256.
/// </summary>
internal sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JsonWebTokenHandler handler = new();

    /// <inheritdoc />
    public AccessToken CreateToken(AuthenticatedUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        JwtOptions jwt = options.Value;
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset expiresAt = now.AddMinutes(jwt.ExpirationMinutes);

        string token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtClaimNames.Subject] = user.Id.ToString(CultureInfo.InvariantCulture),
                [JwtClaimNames.Username] = user.Username,
                [JwtClaimNames.FullName] = user.FullName,
                [JwtClaimNames.Role] = user.Role,
                [JwtClaimNames.TokenId] = Guid.NewGuid().ToString("N"),
            },
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }
}
