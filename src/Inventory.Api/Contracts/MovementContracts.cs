using System.ComponentModel.DataAnnotations;
using Inventory.Application.Models;
using Inventory.Domain.Entities;

namespace Inventory.Api.Contracts;

/// <summary>
/// Datos de una entrada o salida de inventario.
/// </summary>
public sealed record RegisterMovementRequest(
    [Required, Range(1, long.MaxValue)] long? ProductId,
    [Required, Range(1, int.MaxValue)] int? Quantity,
    [MaxLength(Movement.ReasonMaxLength)] string? Reason);

/// <summary>
/// Movimiento registrado y stock resultante.
/// </summary>
public sealed record RegisteredMovementResponse(
    long MovementId,
    long ProductId,
    MovementType Type,
    int Quantity,
    int StockAfter)
{
    /// <summary>
    /// Convierte el resultado del caso de uso en la respuesta de la API.
    /// </summary>
    public static RegisteredMovementResponse From(RegisteredMovement movement)
    {
        ArgumentNullException.ThrowIfNull(movement);

        return new RegisteredMovementResponse(
            movement.MovementId, movement.ProductId, movement.Type, movement.Quantity, movement.StockAfter);
    }
}

/// <summary>
/// Movimiento del historial con datos del producto y del usuario.
/// </summary>
public sealed record MovementResponse(
    long MovementId,
    long ProductId,
    string Sku,
    string ProductName,
    MovementType Type,
    int Quantity,
    int StockAfter,
    string? Reason,
    string Username,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Convierte un elemento del historial en la respuesta de la API.
    /// </summary>
    public static MovementResponse From(MovementHistoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new MovementResponse(
            item.MovementId, item.ProductId, item.Sku, item.ProductName, item.Type,
            item.Quantity, item.StockAfter, item.Reason, item.Username, item.CreatedAt);
    }
}
