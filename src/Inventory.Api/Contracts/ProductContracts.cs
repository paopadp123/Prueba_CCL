using System.ComponentModel.DataAnnotations;
using Inventory.Domain.Entities;

namespace Inventory.Api.Contracts;

/// <summary>
/// Datos para crear un producto.
/// </summary>
public sealed record CreateProductRequest(
    [Required, MaxLength(30)] string Sku,
    [Required, MaxLength(Product.NameMaxLength)] string Name,
    [MaxLength(Product.DescriptionMaxLength)] string? Description,
    [Required, Range(typeof(decimal), "0", "9999999999.99")] decimal? UnitPrice);

/// <summary>
/// Datos editables de un producto; el SKU no se puede cambiar.
/// </summary>
public sealed record UpdateProductRequest(
    [Required, MaxLength(Product.NameMaxLength)] string Name,
    [MaxLength(Product.DescriptionMaxLength)] string? Description,
    [Required, Range(typeof(decimal), "0", "9999999999.99")] decimal? UnitPrice);

/// <summary>
/// Nuevo estado del producto.
/// </summary>
public sealed record ChangeProductStatusRequest([Required] bool? IsActive);

/// <summary>
/// Producto con su stock vigente.
/// </summary>
public sealed record ProductResponse(
    long Id,
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    int CurrentStock,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Convierte la entidad de dominio en la respuesta de la API.
    /// </summary>
    public static ProductResponse From(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new ProductResponse(
            product.Id,
            product.Sku.Value,
            product.Name,
            product.Description,
            product.UnitPrice,
            product.CurrentStock,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt);
    }
}

/// <summary>
/// Página de resultados.
/// </summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount, int TotalPages);
