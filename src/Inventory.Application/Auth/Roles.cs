namespace Inventory.Application.Auth;

/// <summary>
/// Roles de usuario definidos en inventory.users.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Administra el catálogo y registra movimientos.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Consulta el catálogo y registra movimientos.
    /// </summary>
    public const string Operator = "Operator";
}
