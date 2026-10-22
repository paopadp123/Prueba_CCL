namespace Inventory.Domain.Exceptions;

/// <summary>
/// Ya existe un producto con el mismo SKU.
/// </summary>
public sealed class DuplicateSkuException(string sku)
    : DomainException("duplicate_sku", $"Ya existe un producto con el SKU '{sku}'.")
{
    /// <summary>
    /// SKU duplicado.
    /// </summary>
    public string Sku { get; } = sku;
}
