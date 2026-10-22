using Inventory.Api.Contracts;
using Inventory.Api.Security;
using Inventory.Application.Auth;
using Inventory.Application.Models;
using Inventory.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Inventory.Api.Controllers;

/// <summary>
/// Inicio de sesión y datos del usuario actual.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(LoginService loginService) : ControllerBase
{
    /// <summary>
    /// Valida las credenciales y devuelve un token JWT.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimiting.PolicyName)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        LoginResult result = await loginService.LoginAsync(request.Username, request.Password, cancellationToken);
        AuthenticatedUser user = result.User;

        return Ok(new LoginResponse(
            result.AccessToken.Token,
            "Bearer",
            result.AccessToken.ExpiresAt,
            new UserResponse(user.Id, user.Username, user.FullName, user.Role)));
    }

    /// <summary>
    /// Devuelve el usuario dueño del token.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserResponse> Me() => Ok(new UserResponse(
        HttpUserContext.ReadUserId(User) ?? 0,
        User.FindFirst(JwtClaimNames.Username)?.Value ?? string.Empty,
        User.FindFirst(JwtClaimNames.FullName)?.Value ?? string.Empty,
        User.FindFirst(JwtClaimNames.Role)?.Value ?? string.Empty));
}
