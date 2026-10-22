using Inventory.Domain.Entities;

namespace Inventory.Application.Models;

/// <summary>
/// Movimiento registrado, con datos del producto y del usuario.
/// </summary>
public sealed record MovementHistoryItem(
    long MovementId,
    long ProductId,
    string Sku,
    string ProductName,
    MovementType Type,
    int Quantity,
    int StockAfter,
    string? Reason,
    string Username,
    DateTimeOffset CreatedAt);
