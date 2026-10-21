namespace Inventory.Domain.Exceptions;

/// <summary>
/// El SKU no cumple el formato permitido.
/// </summary>
public sealed class InvalidSkuException(string? value)
    : DomainValidationException(
        "invalid_sku",
        $"El SKU '{value}' no es válido: debe tener entre 3 y 30 caracteres, solo letras, números y guiones, y no puede empezar con guion.")
{
    /// <summary>
    /// Valor recibido.
    /// </summary>
    public string? Value { get; } = value;
}
