using Inventory.Application.Models;

namespace Inventory.Application.Auth;

/// <summary>
/// Resultado de un inicio de sesión exitoso.
/// </summary>
public sealed record LoginResult(AccessToken AccessToken, AuthenticatedUser User);
