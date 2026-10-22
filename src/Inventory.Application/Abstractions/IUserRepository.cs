using Inventory.Application.Models;

namespace Inventory.Application.Abstractions;

/// <summary>
/// Acceso a los usuarios del sistema.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Busca un usuario por su nombre de usuario; null si no existe.
    /// </summary>
    Task<UserCredentials?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
}
