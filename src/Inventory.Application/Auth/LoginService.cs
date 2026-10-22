using Inventory.Application.Abstractions;
using Inventory.Application.Models;

namespace Inventory.Application.Auth;

/// <summary>
/// Caso de uso de inicio de sesión: valida las credenciales y emite el token.
/// </summary>
public sealed class LoginService(IUserRepository users, IPasswordHasher passwordHasher, ITokenService tokens)
{
    /// <summary>
    /// Autentica al usuario o lanza <see cref="InvalidCredentialsException"/>.
    /// La contraseña se verifica siempre, aun si el usuario no existe, para que el
    /// tiempo de respuesta no revele qué usuarios existen.
    /// </summary>
    public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        UserCredentials? user = await users.GetByUsernameAsync(username.Trim(), cancellationToken);

        bool passwordMatches = passwordHasher.Verify(password, user?.PasswordHash);
        if (user is null || !passwordMatches || !user.IsActive)
        {
            throw new InvalidCredentialsException();
        }

        AuthenticatedUser authenticated = new(user.Id, user.Username, user.FullName, user.Role);
        return new LoginResult(tokens.CreateToken(authenticated), authenticated);
    }
}
