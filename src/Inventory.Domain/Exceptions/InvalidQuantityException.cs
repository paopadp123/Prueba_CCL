namespace Inventory.Domain.Exceptions;

/// <summary>
/// La cantidad de un movimiento no es mayor que cero.
/// </summary>
public sealed class InvalidQuantityException(int value)
    : DomainValidationException("invalid_quantity", $"La cantidad debe ser mayor que cero (recibido: {value}).")
{
    /// <summary>
    /// Valor recibido.
    /// </summary>
    public int Value { get; } = value;
}
