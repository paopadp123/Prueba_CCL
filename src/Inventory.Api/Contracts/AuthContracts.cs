using System.ComponentModel.DataAnnotations;

namespace Inventory.Api.Contracts;

/// <summary>
/// Credenciales de inicio de sesión.
/// </summary>
public sealed record LoginRequest(
    [Required, MaxLength(50)] string Username,
    [Required, MaxLength(100)] string Password);

/// <summary>
/// Token emitido y datos básicos del usuario.
/// </summary>
public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, UserResponse User);

/// <summary>
/// Datos públicos del usuario autenticado.
/// </summary>
public sealed record UserResponse(long Id, string Username, string FullName, string Role);
