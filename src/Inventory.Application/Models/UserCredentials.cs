namespace Inventory.Application.Models;

/// <summary>
/// Datos de un usuario necesarios para autenticarlo.
/// </summary>
public sealed record UserCredentials(
    long Id,
    string Username,
    string PasswordHash,
    string FullName,
    string Role,
    bool IsActive);
