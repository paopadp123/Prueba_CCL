namespace Inventory.Domain.Exceptions;

/// <summary>
/// El usuario no existe o está inactivo.
/// </summary>
public sealed class UserNotAvailableException(long userId)
    : DomainException("user_not_available", $"El usuario {userId} no existe o está inactivo.")
{
    /// <summary>
    /// Identificador del usuario.
    /// </summary>
    public long UserId { get; } = userId;
}
