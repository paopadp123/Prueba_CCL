using Inventory.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.IntegrationTests.Api;

/// <summary>
/// Levanta la API en memoria apuntando a la base de pruebas y agrega los
/// controladores de apoyo definidos en este proyecto.
/// </summary>
public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    /// <summary>
    /// Clave de firma usada solo en pruebas.
    /// </summary>
    public const string TestSigningKey = "clave-de-pruebas-de-integracion-0123456789";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", connectionString);
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly));
    }
}
