using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Inventory.Infrastructure.Health;

/// <summary>
/// Verifica que PostgreSQL responda y que el esquema del inventario exista.
/// </summary>
internal sealed class DatabaseHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using NpgsqlCommand command = dataSource.CreateCommand("SELECT to_regclass('inventory.products') IS NOT NULL");
            bool schemaReady = (bool)(await command.ExecuteScalarAsync(cancellationToken))!;

            return schemaReady
                ? HealthCheckResult.Healthy("PostgreSQL disponible.")
                : HealthCheckResult.Unhealthy("PostgreSQL responde, pero falta el esquema 'inventory'.");
        }
        catch (NpgsqlException exception)
        {
            return HealthCheckResult.Unhealthy("No se pudo conectar a PostgreSQL.", exception);
        }
    }
}
