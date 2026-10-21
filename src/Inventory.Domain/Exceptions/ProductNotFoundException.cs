namespace Inventory.Domain.Exceptions;

/// <summary>
/// El producto solicitado no existe.
/// </summary>
public sealed class ProductNotFoundException(long productId)
    : DomainException("product_not_found", $"El producto {productId} no existe.")
{
    /// <summary>
    /// Identificador buscado.
    /// </summary>
    public long ProductId { get; } = productId;
}
