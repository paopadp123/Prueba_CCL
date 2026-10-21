namespace Inventory.Application.Models;

/// <summary>
/// Página solicitada; normaliza valores fuera de rango.
/// </summary>
public sealed record PageRequest
{
    /// <summary>
    /// Tamaño de página máximo permitido.
    /// </summary>
    public const int MaxPageSize = 200;

    /// <summary>
    /// Crea la solicitud: página mínima 1 y tamaño entre 1 y <see cref="MaxPageSize"/>.
    /// </summary>
    public PageRequest(int page, int pageSize)
    {
        Page = Math.Max(page, 1);
        PageSize = Math.Clamp(pageSize, 1, MaxPageSize);
    }

    /// <summary>
    /// Número de página, desde 1.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Elementos por página.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Filas a omitir.
    /// </summary>
    public int Offset => (Page - 1) * PageSize;
}
