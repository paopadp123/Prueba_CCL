namespace Inventory.Application.Models;

/// <summary>
/// Token de acceso emitido y su fecha de vencimiento.
/// </summary>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
