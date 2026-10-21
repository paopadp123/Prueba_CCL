using Inventory.Application.Models;

namespace Inventory.Application.Abstractions;

/// <summary>
/// Consultas de solo lectura sobre stock e historial.
/// </summary>
public interface IInventoryQueries
{
    /// <summary>
    /// Stock actual paginado, filtrado opcionalmente por SKU o nombre.
    /// </summary>
    Task<PagedResult<StockItem>> GetStockAsync(
        string? search, bool onlyActive, PageRequest page, CancellationToken cancellationToken);

    /// <summary>
    /// Historial de movimientos paginado, filtrado opcionalmente por producto.
    /// </summary>
    Task<PagedResult<MovementHistoryItem>> GetMovementsAsync(
        long? productId, PageRequest page, CancellationToken cancellationToken);
}
