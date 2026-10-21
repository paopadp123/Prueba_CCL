using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Códigos de tipo de movimiento usados en la base de datos.
/// </summary>
internal static class MovementTypeCodes
{
    /// <summary>
    /// Convierte un código de la base de datos (IN u OUT) al enum del dominio.
    /// </summary>
    public static MovementType FromCode(string code) => code switch
    {
        "IN" => MovementType.Entry,
        "OUT" => MovementType.Exit,
        _ => throw new InvalidOperationException($"Tipo de movimiento desconocido: '{code}'."),
    };
}
