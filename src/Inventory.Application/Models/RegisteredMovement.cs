using Inventory.Domain.Entities;

namespace Inventory.Application.Models;

/// <summary>
/// Resultado de registrar un movimiento.
/// </summary>
public sealed record RegisteredMovement(
    long MovementId,
    long ProductId,
    MovementType Type,
    int Quantity,
    int StockAfter);
