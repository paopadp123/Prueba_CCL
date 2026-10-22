using Inventory.Application.Models;

namespace Inventory.Application.Abstractions;

/// <summary>
/// Emisión de tokens de acceso.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Crea un token firmado para el usuario autenticado.
    /// </summary>
    AccessToken CreateToken(AuthenticatedUser user);
}
