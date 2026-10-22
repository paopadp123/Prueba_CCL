using Inventory.Application.Models;

namespace Inventory.Api.Contracts;

/// <summary>
/// Stock actual de un producto y su valor (precio unitario × unidades).
/// </summary>
public sealed record StockResponse(
    long ProductId,
    string Sku,
    string Name,
    decimal UnitPrice,
    int CurrentStock,
    decimal StockValue,
    bool IsActive,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Convierte el modelo de lectura en la respuesta pública.
    /// </summary>
    public static StockResponse From(StockItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new StockResponse(
            item.ProductId,
            item.Sku,
            item.Name,
            item.UnitPrice,
            item.CurrentStock,
            item.StockValue,
            item.IsActive,
            item.UpdatedAt);
    }
}
