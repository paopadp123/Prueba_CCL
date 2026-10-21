namespace Inventory.Domain.Exceptions;

/// <summary>
/// Excepción base para las violaciones de reglas de negocio.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Crea la excepción con un código estable y un mensaje para el usuario.
    /// </summary>
    protected DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Código estable del error, pensado para que lo consuman los clientes de la API.
    /// </summary>
    public string Code { get; }
}
