namespace Inventory.Domain.Exceptions;

/// <summary>
/// La salida solicitada supera el stock disponible.
/// </summary>
public sealed class InsufficientStockException(int available, int requested)
    : DomainException("insufficient_stock", $"Stock insuficiente: disponible {available}, solicitado {requested}.")
{
    /// <summary>
    /// Stock disponible al momento de la validación.
    /// </summary>
    public int Available { get; } = available;

    /// <summary>
    /// Cantidad solicitada.
    /// </summary>
    public int Requested { get; } = requested;
}
