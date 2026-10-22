using Dapper;
using Inventory.Application.Abstractions;
using Inventory.Application.Models;
using Npgsql;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Consulta de usuarios con Dapper sobre inventory.users.
/// </summary>
internal sealed class UserRepository(NpgsqlDataSource dataSource) : IUserRepository
{
    /// <inheritdoc />
    public async Task<UserCredentials?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<UserCredentials>(new CommandDefinition(
            """
            SELECT id AS Id, username AS Username, password_hash AS PasswordHash,
                   full_name AS FullName, role AS Role, is_active AS IsActive
            FROM inventory.users
            WHERE username = @Username
            """,
            new { Username = username },
            cancellationToken: cancellationToken));
    }
}
