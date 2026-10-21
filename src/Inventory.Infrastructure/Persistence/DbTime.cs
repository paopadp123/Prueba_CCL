namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Conversión de fechas TIMESTAMPTZ, que Npgsql entrega como DateTime en UTC.
/// </summary>
internal static class DbTime
{
    /// <summary>
    /// Convierte una fecha UTC de la base de datos a <see cref="DateTimeOffset"/>.
    /// </summary>
    public static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
