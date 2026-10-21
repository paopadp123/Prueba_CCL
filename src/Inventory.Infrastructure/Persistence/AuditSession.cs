using System.Globalization;
using Dapper;
using Inventory.Application.Abstractions;
using Npgsql;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Abre una transacción que informa a la auditoría qué usuario de la aplicación hace el cambio.
/// </summary>
internal static class AuditSession
{
    /// <summary>
    /// Inicia la transacción y fija app.user_id solo para ella (equivale a SET LOCAL).
    /// </summary>
    public static async Task<NpgsqlTransaction> BeginAsync(
        NpgsqlConnection connection, IUserContext userContext, CancellationToken cancellationToken)
    {
        var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (userContext.UserId is { } userId)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "SELECT set_config('app.user_id', @UserId, TRUE)",
                new { UserId = userId.ToString(CultureInfo.InvariantCulture) },
                transaction,
                cancellationToken: cancellationToken));
        }

        return transaction;
    }
}
