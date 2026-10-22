using System.Globalization;
using Inventory.Domain.Exceptions;
using Npgsql;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Traduce los errores de PostgreSQL (SQLSTATE) a excepciones de dominio.
/// Los códigos IN001 a IN005 los lanzan los procedimientos de 03_procedures.sql.
/// </summary>
public static class PostgresErrorMapper
{
    private const string ProductNotFound = "IN001";
    private const string InsufficientStock = "IN002";
    private const string InactiveProduct = "IN003";
    private const string UserNotAvailable = "IN004";
    private const string InvalidQuantity = "IN005";
    private const string SkuUniqueIndex = "ux_products_sku";

    /// <summary>
    /// Devuelve la excepción de dominio equivalente o la excepción original si no hay traducción.
    /// Los parámetros opcionales aportan el contexto de la operación para el mensaje.
    /// </summary>
    public static Exception Map(
        PostgresException exception,
        long productId = 0,
        long userId = 0,
        int quantity = 0,
        string? sku = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.SqlState switch
        {
            ProductNotFound => new ProductNotFoundException(productId),
            InsufficientStock => MapInsufficientStock(exception.Detail, quantity),
            InactiveProduct => new InactiveProductException(productId),
            UserNotAvailable => new UserNotAvailableException(userId),
            InvalidQuantity => new InvalidQuantityException(quantity),
            PostgresErrorCodes.UniqueViolation when exception.ConstraintName == SkuUniqueIndex
                => new DuplicateSkuException(sku ?? string.Empty),
            _ => exception,
        };
    }

    private static InsufficientStockException MapInsufficientStock(string? detail, int quantity)
    {
        Dictionary<string, string> values = (detail ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(pair => pair.Length == 2)
            .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim(), StringComparer.Ordinal);

        return new InsufficientStockException(
            ReadInt(values, "available", 0),
            ReadInt(values, "requested", quantity));
    }

    private static int ReadInt(Dictionary<string, string> values, string key, int fallback) =>
        values.TryGetValue(key, out string? text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : fallback;
}
