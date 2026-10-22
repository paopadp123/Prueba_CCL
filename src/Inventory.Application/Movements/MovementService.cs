using Inventory.Application.Abstractions;
using Inventory.Application.Models;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.ValueObjects;

namespace Inventory.Application.Movements;

/// <summary>
/// Casos de uso de movimientos de inventario. La validación definitiva de existencia,
/// estado y stock la hace el procedimiento almacenado bajo bloqueo de fila, para que dos
/// salidas simultáneas no puedan dejar el stock negativo.
/// </summary>
public sealed class MovementService(
    IMovementRepository movements,
    IProductRepository products,
    IInventoryQueries queries,
    IUserContext userContext)
{
    /// <summary>
    /// Registra una entrada de stock.
    /// </summary>
    public Task<RegisteredMovement> RegisterEntryAsync(
        long productId, int quantity, string? reason, CancellationToken cancellationToken) =>
        RegisterAsync(productId, MovementType.Entry, quantity, reason, cancellationToken);

    /// <summary>
    /// Registra una salida de stock; lanza <see cref="InsufficientStockException"/> si no alcanza.
    /// </summary>
    public Task<RegisteredMovement> RegisterExitAsync(
        long productId, int quantity, string? reason, CancellationToken cancellationToken) =>
        RegisterAsync(productId, MovementType.Exit, quantity, reason, cancellationToken);

    /// <summary>
    /// Historial paginado de todos los movimientos o de un producto, del más reciente al más antiguo.
    /// </summary>
    public async Task<PagedResult<MovementHistoryItem>> GetHistoryAsync(
        long? productId, PageRequest page, CancellationToken cancellationToken)
    {
        if (productId is long id && await products.GetByIdAsync(id, cancellationToken) is null)
        {
            throw new ProductNotFoundException(id);
        }

        return await queries.GetMovementsAsync(productId, page, cancellationToken);
    }

    private Task<RegisteredMovement> RegisterAsync(
        long productId, MovementType type, int quantity, string? reason, CancellationToken cancellationToken)
    {
        long userId = userContext.UserId
            ?? throw new InvalidOperationException("No hay un usuario autenticado para registrar el movimiento.");

        Movement movement = Movement.Create(productId, type, Quantity.Create(quantity), userId, reason);
        return movements.RegisterAsync(movement, cancellationToken);
    }
}
