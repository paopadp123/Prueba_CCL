namespace Inventory.Application.Auth;

/// <summary>
/// Usuario o contraseña incorrectos, o usuario inactivo.
/// El mensaje es el mismo en todos los casos para no revelar qué dato falló.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    /// <summary>
    /// Código estable del error.
    /// </summary>
    public const string Code = "invalid_credentials";

    /// <summary>
    /// Crea la excepción con el mensaje genérico.
    /// </summary>
    public InvalidCredentialsException()
        : base("Usuario o contraseña incorrectos.")
    {
    }
}
