using System.Text.RegularExpressions;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.ValueObjects;

/// <summary>
/// Código único de producto: 3 a 30 caracteres en mayúsculas, números y guiones.
/// Usa el mismo formato que la restricción ck_products_sku_format de la base de datos.
/// </summary>
public sealed partial record Sku
{
    private Sku(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Valor normalizado del SKU.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Normaliza (recorta y pasa a mayúsculas) y valida el SKU, o lanza <see cref="InvalidSkuException"/>.
    /// </summary>
    public static Sku Create(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();

        return normalized is not null && SkuFormat().IsMatch(normalized)
            ? new Sku(normalized)
            : throw new InvalidSkuException(value);
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]{2,29}$")]
    private static partial Regex SkuFormat();
}
