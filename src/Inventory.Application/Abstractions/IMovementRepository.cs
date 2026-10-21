using Inventory.Application.Models;
using Inventory.Domain.Entities;

namespace Inventory.Application.Abstractions;

/// <summary>
/// Registro de movimientos de inventario.
/// </summary>
public interface IMovementRepository
{
    /// <summary>
    /// Registra una entrada o salida y devuelve el stock resultante.
    /// Traduce los errores de negocio de la base de datos a excepciones de dominio.
    /// </summary>
    Task<RegisteredMovement> RegisterAsync(Movement movement, CancellationToken cancellationToken);
}
