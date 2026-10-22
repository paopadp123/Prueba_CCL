namespace Inventory.Application.Abstractions;

/// <summary>
/// Usuario autenticado de la petición actual.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Identificador del usuario, o nulo si la petición es anónima.
    /// </summary>
    long? UserId { get; }
}
