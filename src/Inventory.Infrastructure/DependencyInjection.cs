using Inventory.Application.Abstractions;
using Inventory.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Inventory.Infrastructure;

/// <summary>
/// Registro de los servicios de infraestructura.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Nombre de la cadena de conexión en la configuración.
    /// </summary>
    public const string ConnectionStringName = "Inventory";

    /// <summary>
    /// Registra el origen de datos de PostgreSQL, los repositorios y las consultas.
    /// Habilita IncludeErrorDetail porque los procedimientos envían datos de negocio en el DETAIL
    /// del error (por ejemplo, el stock disponible); esos errores nunca se exponen tal cual al cliente.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Falta la cadena de conexión '{ConnectionStringName}'. Configúrela con dotnet user-secrets o variables de entorno.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString) { IncludeErrorDetail = true };

        services.AddNpgsqlDataSource(builder.ConnectionString);
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IMovementRepository, MovementRepository>();
        services.AddScoped<IInventoryQueries, InventoryQueries>();

        return services;
    }
}
