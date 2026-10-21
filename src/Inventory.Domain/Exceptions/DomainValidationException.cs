namespace Inventory.Domain.Exceptions;

/// <summary>
/// Datos que no cumplen las invariantes del dominio.
/// </summary>
public class DomainValidationException : DomainException
{
    /// <summary>
    /// Crea la excepción con el código genérico de validación.
    /// </summary>
    public DomainValidationException(string message)
        : this("validation_error", message)
    {
    }

    /// <summary>
    /// Crea la excepción con un código de validación específico.
    /// </summary>
    protected DomainValidationException(string code, string message)
        : base(code, message)
    {
    }
}
