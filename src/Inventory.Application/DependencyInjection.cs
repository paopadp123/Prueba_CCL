using Inventory.Application.Auth;
using Inventory.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

/// <summary>
/// Registro de los casos de uso de la aplicación.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios de Application.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginService>();
        services.AddScoped<ProductService>();
        return services;
    }
}
