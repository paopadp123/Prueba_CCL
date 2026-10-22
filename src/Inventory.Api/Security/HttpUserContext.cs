using System.Globalization;
using System.Security.Claims;
using Inventory.Application.Abstractions;

namespace Inventory.Api.Security;

/// <summary>
/// Obtiene el usuario autenticado a partir de los claims de la solicitud.
/// </summary>
public sealed class HttpUserContext(IHttpContextAccessor accessor) : IUserContext
{
    /// <summary>
    /// Claim estándar de JWT con el identificador del usuario.
    /// </summary>
    public const string SubjectClaim = "sub";

    /// <inheritdoc />
    public long? UserId => ReadUserId(accessor.HttpContext?.User);

    /// <summary>
    /// Lee el identificador desde "sub" o, si el token fue mapeado, desde NameIdentifier.
    /// </summary>
    public static long? ReadUserId(ClaimsPrincipal? user)
    {
        string? value = user?.FindFirst(SubjectClaim)?.Value ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long id) && id > 0
            ? id
            : null;
    }
}
