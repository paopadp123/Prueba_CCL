using System.Text.RegularExpressions;

namespace Inventory.Api.Observability;

/// <summary>
/// Reglas del identificador de correlación que viaja en el encabezado X-Correlation-ID.
/// </summary>
public static partial class CorrelationId
{
    /// <summary>
    /// Encabezado HTTP de entrada y salida.
    /// </summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// Nombre de la propiedad en los logs.
    /// </summary>
    public const string LogProperty = "CorrelationId";

    /// <summary>
    /// Usa el valor recibido si es seguro para los logs; si no, genera uno nuevo.
    /// </summary>
    public static string Resolve(string? incoming) =>
        incoming is not null && AllowedFormat().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString("N");

    /// <summary>
    /// Identificador de la solicitud en curso.
    /// </summary>
    public static string Get(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.TraceIdentifier;
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex AllowedFormat();
}
