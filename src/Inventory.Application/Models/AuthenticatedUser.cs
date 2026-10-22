namespace Inventory.Application.Models;

/// <summary>
/// Usuario que inició sesión correctamente.
/// </summary>
public sealed record AuthenticatedUser(long Id, string Username, string FullName, string Role);
