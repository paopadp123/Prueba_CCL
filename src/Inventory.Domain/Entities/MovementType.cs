namespace Inventory.Domain.Entities;

/// <summary>
/// Tipo de movimiento de inventario.
/// </summary>
public enum MovementType
{
    /// <summary>
    /// Entrada: suma unidades al stock.
    /// </summary>
    Entry = 1,

    /// <summary>
    /// Salida: descuenta unidades del stock.
    /// </summary>
    Exit = 2,
}
