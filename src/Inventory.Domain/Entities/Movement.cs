using Inventory.Domain.Exceptions;
using Inventory.Domain.ValueObjects;

namespace Inventory.Domain.Entities;

/// <summary>
/// Movimiento de inventario (entrada o salida) sobre un producto.
/// </summary>
public sealed class Movement
{
    /// <summary>
    /// Longitud máxima del motivo.
    /// </summary>
    public const int ReasonMaxLength = 250;

    private Movement(long productId, MovementType type, Quantity quantity, long userId, string? reason)
    {
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        UserId = userId;
        Reason = reason;
    }

    /// <summary>
    /// Identificador; 0 mientras no se haya registrado.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Producto afectado.
    /// </summary>
    public long ProductId { get; }

    /// <summary>
    /// Entrada o salida.
    /// </summary>
    public MovementType Type { get; }

    /// <summary>
    /// Unidades movidas.
    /// </summary>
    public Quantity Quantity { get; }

    /// <summary>
    /// Usuario que registra el movimiento.
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// Motivo opcional.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Stock resultante; nulo mientras no se haya registrado.
    /// </summary>
    public int? StockAfter { get; private set; }

    /// <summary>
    /// Fecha de registro.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <summary>
    /// Crea un movimiento pendiente de registrar.
    /// </summary>
    public static Movement Create(long productId, MovementType type, Quantity quantity, long userId, string? reason)
    {
        ArgumentNullException.ThrowIfNull(quantity);

        if (productId <= 0)
        {
            throw new DomainValidationException("El producto del movimiento no es válido.");
        }

        if (userId <= 0)
        {
            throw new DomainValidationException("El usuario del movimiento no es válido.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainValidationException("El tipo de movimiento no es válido.");
        }

        return new Movement(productId, type, quantity, userId, NormalizeReason(reason));
    }

    /// <summary>
    /// Reconstruye un movimiento ya registrado a partir de los datos persistidos.
    /// </summary>
    public static Movement Rehydrate(
        long id,
        long productId,
        MovementType type,
        int quantity,
        int stockAfter,
        string? reason,
        long userId,
        DateTimeOffset createdAt) =>
        new(productId, type, Quantity.Create(quantity), userId, reason)
        {
            Id = id,
            StockAfter = stockAfter,
            CreatedAt = createdAt,
        };

    private static string? NormalizeReason(string? reason)
    {
        string? trimmed = reason?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length <= ReasonMaxLength
            ? trimmed
            : throw new DomainValidationException($"El motivo no puede superar {ReasonMaxLength} caracteres.");
    }
}
