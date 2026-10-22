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

        await using NpgsqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
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
        IConfigurationRoot configuration = new ConfigurationBuilder()
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
        await using NpgsqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(sql, connection);
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return (T)(await command.ExecuteScalarAsync())!;
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        NpgsqlConnectionStringBuilder builder = new(ConnectionString);
        string databaseName = builder.Database!;
        builder.Database = "postgres";

        await using NpgsqlConnection connection = new(builder.ConnectionString);
        await connection.OpenAsync();

        await using NpgsqlCommand exists = new("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        exists.Parameters.AddWithValue("name", databaseName);
        if (await exists.ExecuteScalarAsync() is null)
        {
            await using NpgsqlCommand create = new($"CREATE DATABASE \"{databaseName}\"", connection);
            await create.ExecuteNonQueryAsync();
        }
    }

    private async Task ResetSchemaAsync()
    {
        await using NpgsqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        await using (NpgsqlCommand drop = new NpgsqlCommand("DROP SCHEMA IF EXISTS inventory CASCADE", connection))
        {
            await drop.ExecuteNonQueryAsync();
        }

        IOrderedEnumerable<string> scripts = Directory.GetFiles(Path.Combine(FindRepositoryRoot(), "database"), "0*.sql").Order(StringComparer.Ordinal);
        foreach (string? script in scripts)
        {
            await using NpgsqlCommand command = new(await File.ReadAllTextAsync(script), connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
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
