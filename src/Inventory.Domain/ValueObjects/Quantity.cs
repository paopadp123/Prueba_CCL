using Inventory.Domain.Exceptions;

namespace Inventory.Domain.ValueObjects;

/// <summary>
/// Cantidad de unidades de un movimiento; siempre mayor que cero.
/// </summary>
public sealed record Quantity
{
    private Quantity(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Número de unidades.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Crea una cantidad válida o lanza <see cref="InvalidQuantityException"/>.
    /// </summary>
    public static Quantity Create(int value) =>
        value > 0 ? new Quantity(value) : throw new InvalidQuantityException(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
