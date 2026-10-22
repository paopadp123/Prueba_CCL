using Dapper;
using Inventory.Application.Abstractions;
using Inventory.Application.Models;
using Npgsql;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Consultas de stock e historial sobre fn_get_stock y fn_get_movements.
/// </summary>
internal sealed class InventoryQueries(NpgsqlDataSource dataSource) : IInventoryQueries
{
    /// <inheritdoc />
    public async Task<PagedResult<StockItem>> GetStockAsync(
        string? search, bool onlyActive, PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        List<StockRow> rows = (await connection.QueryAsync<StockRow>(new CommandDefinition(
            """
            SELECT product_id AS ProductId, sku AS Sku, name AS Name, unit_price AS UnitPrice,
                   current_stock AS CurrentStock, stock_value AS StockValue, is_active AS IsActive,
                   updated_at AS UpdatedAt, total_count AS TotalCount
            FROM inventory.fn_get_stock(@Search, @OnlyActive, @Limit, @Offset)
            """,
            new { Search = NormalizeSearch(search), OnlyActive = onlyActive, Limit = page.PageSize, page.Offset },
            cancellationToken: cancellationToken))).ToList();

        List<StockItem> items = rows
            .Select(row => new StockItem(
                row.ProductId, row.Sku, row.Name, row.UnitPrice, row.CurrentStock, row.StockValue,
                row.IsActive, DbTime.ToOffset(row.UpdatedAt)))
            .ToList();

        return new PagedResult<StockItem>(items, page.Page, page.PageSize, rows.FirstOrDefault()?.TotalCount ?? 0);
    }

    /// <inheritdoc />
    public async Task<PagedResult<MovementHistoryItem>> GetMovementsAsync(
        long? productId, PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        List<MovementRow> rows = (await connection.QueryAsync<MovementRow>(new CommandDefinition(
            """
            SELECT movement_id AS MovementId, product_id AS ProductId, sku AS Sku, product_name AS ProductName,
                   movement_type AS MovementType, quantity AS Quantity, stock_after AS StockAfter, reason AS Reason,
                   username AS Username, created_at AS CreatedAt, total_count AS TotalCount
            FROM inventory.fn_get_movements(@ProductId, @Limit, @Offset)
            """,
            new { ProductId = productId, Limit = page.PageSize, page.Offset },
            cancellationToken: cancellationToken))).ToList();

        List<MovementHistoryItem> items = rows
            .Select(row => new MovementHistoryItem(
                row.MovementId, row.ProductId, row.Sku, row.ProductName,
                MovementTypeCodes.FromCode(row.MovementType), row.Quantity, row.StockAfter, row.Reason,
                row.Username, DbTime.ToOffset(row.CreatedAt)))
            .ToList();

        return new PagedResult<MovementHistoryItem>(
            items, page.Page, page.PageSize, rows.FirstOrDefault()?.TotalCount ?? 0);
    }

    private static string? NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    private sealed record StockRow(
        long ProductId,
        string Sku,
        string Name,
        decimal UnitPrice,
        int CurrentStock,
        decimal StockValue,
        bool IsActive,
        DateTime UpdatedAt,
        long TotalCount);

    private sealed record MovementRow(
        long MovementId,
        long ProductId,
        string Sku,
        string ProductName,
        string MovementType,
        int Quantity,
        int StockAfter,
        string? Reason,
        string Username,
        DateTime CreatedAt,
        long TotalCount);
}
