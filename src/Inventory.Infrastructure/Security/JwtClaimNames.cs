namespace Inventory.Infrastructure.Security;

/// <summary>
/// Nombres de los claims que lleva el token.
/// </summary>
public static class JwtClaimNames
{
    /// <summary>
    /// Identificador del usuario.
    /// </summary>
    public const string Subject = "sub";

    /// <summary>
    /// Nombre de usuario.
    /// </summary>
    public const string Username = "unique_name";

    /// <summary>
    /// Nombre completo.
    /// </summary>
    public const string FullName = "name";

    /// <summary>
    /// Rol del usuario.
    /// </summary>
    public const string Role = "role";

    /// <summary>
    /// Identificador único del token.
    /// </summary>
    public const string TokenId = "jti";
}
