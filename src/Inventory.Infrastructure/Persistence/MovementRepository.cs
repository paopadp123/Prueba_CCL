using Dapper;
using Inventory.Application.Abstractions;
using Inventory.Application.Models;
using Inventory.Domain.Entities;
using Npgsql;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Registra movimientos llamando a sp_register_entry y sp_register_exit.
/// </summary>
internal sealed class MovementRepository(NpgsqlDataSource dataSource) : IMovementRepository
{
    /// <inheritdoc />
    public async Task<RegisteredMovement> RegisterAsync(Movement movement, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(movement);

        var procedure = movement.Type == MovementType.Entry
            ? "inventory.sp_register_entry"
            : "inventory.sp_register_exit";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        try
        {
            var result = await connection.QuerySingleAsync<(long MovementId, int StockAfter)>(new CommandDefinition(
                $"""
                CALL {procedure}(@ProductId, @Quantity, @UserId, @Reason, NULL, NULL)
                """,
                new { movement.ProductId, Quantity = movement.Quantity.Value, movement.UserId, movement.Reason },
                cancellationToken: cancellationToken));

            return new RegisteredMovement(
                result.MovementId, movement.ProductId, movement.Type, movement.Quantity.Value, result.StockAfter);
        }
        catch (PostgresException exception)
        {
            throw PostgresErrorMapper.Map(exception, movement.ProductId, movement.UserId, movement.Quantity.Value);
        }
    }
}
