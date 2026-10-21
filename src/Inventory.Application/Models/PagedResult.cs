namespace Inventory.Application.Models;

/// <summary>
/// Página de resultados con el total de elementos del filtro.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
    /// <summary>
    /// Número total de páginas.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
