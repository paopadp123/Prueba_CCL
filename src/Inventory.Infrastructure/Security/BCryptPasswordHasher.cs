using Inventory.Application.Abstractions;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Inventory.Infrastructure.Security;

/// <summary>
/// Verificación con BCrypt. Es compatible con los hashes $2a$ que genera pgcrypto en el seed.
/// </summary>
internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    private static readonly string DummyHash = BCryptNet.HashPassword(Guid.NewGuid().ToString(), workFactor: 11);

    /// <inheritdoc />
    public bool Verify(string password, string? passwordHash)
    {
        if (passwordHash is null)
        {
            BCryptNet.Verify(password, DummyHash);
            return false;
        }

        try
        {
            return BCryptNet.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
