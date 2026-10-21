namespace Inventory.Application.Models;

/// <summary>
/// Stock actual de un producto.
/// </summary>
public sealed record StockItem(
    long ProductId,
    string Sku,
    string Name,
    decimal UnitPrice,
    int CurrentStock,
    decimal StockValue,
    bool IsActive,
    DateTimeOffset UpdatedAt);
