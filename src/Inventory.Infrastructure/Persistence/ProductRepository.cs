using Dapper;
using Inventory.Application.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Npgsql;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Persistencia de productos con Dapper sobre inventory.products.
/// El stock no se escribe aquí: solo lo modifican los procedimientos de movimientos.
/// </summary>
internal sealed class ProductRepository(NpgsqlDataSource dataSource, IUserContext userContext) : IProductRepository
{
    private const string Columns = """
        id AS Id, sku AS Sku, name AS Name, description AS Description, unit_price AS UnitPrice,
        current_stock AS CurrentStock, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
        """;

    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ProductRow>(new CommandDefinition(
            $"SELECT {Columns} FROM inventory.products WHERE id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    /// <inheritdoc />
    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await AuditSession.BeginAsync(connection, userContext, cancellationToken);

        try
        {
            var row = await connection.QuerySingleAsync<ProductRow>(new CommandDefinition(
                $"""
                INSERT INTO inventory.products (sku, name, description, unit_price, is_active)
                VALUES (@Sku, @Name, @Description, @UnitPrice, @IsActive)
                RETURNING {Columns}
                """,
                new { Sku = product.Sku.Value, product.Name, product.Description, product.UnitPrice, product.IsActive },
                transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return row.ToDomain();
        }
        catch (PostgresException exception)
        {
            throw PostgresErrorMapper.Map(exception, sku: product.Sku.Value);
        }
    }

    /// <inheritdoc />
    public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await AuditSession.BeginAsync(connection, userContext, cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ProductRow>(new CommandDefinition(
            $"""
            UPDATE inventory.products
            SET name = @Name, description = @Description, unit_price = @UnitPrice, is_active = @IsActive
            WHERE id = @Id
            RETURNING {Columns}
            """,
            new { product.Id, product.Name, product.Description, product.UnitPrice, product.IsActive },
            transaction,
            cancellationToken: cancellationToken))
            ?? throw new ProductNotFoundException(product.Id);

        await transaction.CommitAsync(cancellationToken);
        return row.ToDomain();
    }

    private sealed record ProductRow(
        long Id,
        string Sku,
        string Name,
        string? Description,
        decimal UnitPrice,
        int CurrentStock,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public Product ToDomain() => Product.Rehydrate(
            Id, Sku, Name, Description, UnitPrice, CurrentStock, IsActive,
            DbTime.ToOffset(CreatedAt), DbTime.ToOffset(UpdatedAt));
    }
}
