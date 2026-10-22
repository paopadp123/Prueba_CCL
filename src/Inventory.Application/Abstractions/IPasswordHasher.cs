namespace Inventory.Application.Abstractions;

/// <summary>
/// Verificación de contraseñas contra su hash.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Indica si la contraseña corresponde al hash. Con un hash null devuelve false,
    /// pero tarda lo mismo que una verificación real para no revelar si el usuario existe.
    /// </summary>
    bool Verify(string password, string? passwordHash);
}
