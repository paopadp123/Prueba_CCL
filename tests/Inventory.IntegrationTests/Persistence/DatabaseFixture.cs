using Inventory.Application.Abstractions;
using Inventory.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Inventory.IntegrationTests.Persistence;

/// <summary>
/// Base de datos de pruebas recreada desde los scripts de database/ al iniciar la ejecución.
/// La cadena de conexión se toma de la variable de entorno INVENTORY_TEST_CONNECTION.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string ConnectionVariable = "INVENTORY_TEST_CONNECTION";
    private const string DefaultConnection =
        "Host=localhost;Port=5432;Database=inventory_test;Username=postgres;Password=postgres";

    /// <summary>
    /// Cadena de conexión a la base de pruebas.
    /// </summary>
    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable(ConnectionVariable) ?? DefaultConnection;

    /// <summary>
    /// Identificador del usuario admin creado por el seed.
    /// </summary>
    public long AdminUserId { get; private set; }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await EnsureDatabaseExistsAsync();
        await ResetSchemaAsync();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT id FROM inventory.users WHERE username = 'admin'", connection);
        AdminUserId = (long)(await command.ExecuteScalarAsync())!;
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Crea un contenedor de servicios con la infraestructura real y el usuario indicado.
    /// </summary>
    public ServiceProvider CreateServices(long? userId = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = ConnectionString,
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IUserContext>(new FixedUserContext(userId))
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    /// <summary>
    /// Ejecuta una consulta escalar directa contra la base de pruebas.
    /// </summary>
    public async Task<T> ScalarAsync<T>(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return (T)(await command.ExecuteScalarAsync())!;
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString);
        var databaseName = builder.Database!;
        builder.Database = "postgres";

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        exists.Parameters.AddWithValue("name", databaseName);
        if (await exists.ExecuteScalarAsync() is null)
        {
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
            await create.ExecuteNonQueryAsync();
        }
    }

    private async Task ResetSchemaAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using (var drop = new NpgsqlCommand("DROP SCHEMA IF EXISTS inventory CASCADE", connection))
        {
            await drop.ExecuteNonQueryAsync();
        }

        var scripts = Directory.GetFiles(Path.Combine(FindRepositoryRoot(), "database"), "0*.sql").Order(StringComparer.Ordinal);
        foreach (var script in scripts)
        {
            await using var command = new NpgsqlCommand(await File.ReadAllTextAsync(script), connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Inventory.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }

    private sealed class FixedUserContext(long? userId) : IUserContext
    {
        public long? UserId { get; } = userId;
    }
}

/// <summary>
/// Agrupa las pruebas que comparten la base de datos de pruebas.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseTestGroup : ICollectionFixture<DatabaseFixture>
{
    /// <summary>
    /// Nombre de la colección.
    /// </summary>
    public const string Name = "Database";
}
