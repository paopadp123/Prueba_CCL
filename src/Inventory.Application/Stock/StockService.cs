using Inventory.Application.Abstractions;
using Inventory.Application.Models;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;

namespace Inventory.Application.Stock;

/// <summary>
/// Consultas del stock actual. El stock solo cambia con movimientos; aquí únicamente se lee.
/// </summary>
public sealed class StockService(IInventoryQueries queries, IProductRepository products)
{
    /// <summary>
    /// Stock paginado, filtrando por SKU o nombre; los inactivos solo se incluyen si se piden.
    /// </summary>
    public Task<PagedResult<StockItem>> ListAsync(
        string? search, bool includeInactive, PageRequest page, CancellationToken cancellationToken) =>
        queries.GetStockAsync(search, !includeInactive, page, cancellationToken);

    /// <summary>
    /// Stock de un producto, activo o no; lanza <see cref="ProductNotFoundException"/> si no existe.
    /// </summary>
    public async Task<StockItem> GetByProductAsync(long productId, CancellationToken cancellationToken)
    {
        Product product = await products.GetByIdAsync(productId, cancellationToken)
            ?? throw new ProductNotFoundException(productId);

        return new StockItem(
            product.Id,
            product.Sku.Value,
            product.Name,
            product.UnitPrice,
            product.CurrentStock,
            product.UnitPrice * product.CurrentStock,
            product.IsActive,
            product.UpdatedAt);
    }
}
