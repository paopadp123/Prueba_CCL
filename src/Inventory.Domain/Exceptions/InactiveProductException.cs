namespace Inventory.Domain.Exceptions;

/// <summary>
/// El producto está inactivo y no admite movimientos.
/// </summary>
public sealed class InactiveProductException(long productId)
    : DomainException("inactive_product", $"El producto {productId} está inactivo.")
{
    /// <summary>
    /// Identificador del producto.
    /// </summary>
    public long ProductId { get; } = productId;
}
