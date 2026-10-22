namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Normalización del texto de búsqueda que se envía a las consultas.
/// </summary>
internal static class SqlSearch
{
    /// <summary>
    /// Devuelve null si no hay texto útil; si no, el texto sin espacios en los extremos.
    /// </summary>
    public static string? Normalize(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();
}
