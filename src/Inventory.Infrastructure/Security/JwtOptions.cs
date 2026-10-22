using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Inventory.Infrastructure.Security;

/// <summary>
/// Configuración de los tokens JWT (sección "Jwt").
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Nombre de la sección en la configuración.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Emisor del token.
    /// </summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Destinatario del token.
    /// </summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Clave HMAC de firma; mínimo 32 caracteres. Nunca va en el repositorio.
    /// </summary>
    [Required(ErrorMessage = "Falta Jwt:SigningKey. Configúrela con dotnet user-secrets o la variable Jwt__SigningKey.")]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey debe tener al menos 32 caracteres.")]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Minutos de vigencia del token.
    /// </summary>
    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Crea la clave simétrica a partir de <see cref="SigningKey"/>.
    /// </summary>
    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
