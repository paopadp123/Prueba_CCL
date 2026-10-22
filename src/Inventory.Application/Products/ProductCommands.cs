namespace Inventory.Application.Products;

/// <summary>
/// Datos para crear un producto.
/// </summary>
public sealed record CreateProductCommand(string Sku, string Name, string? Description, decimal UnitPrice);

/// <summary>
/// Datos editables de un producto; el SKU no cambia.
/// </summary>
public sealed record UpdateProductCommand(string Name, string? Description, decimal UnitPrice);
