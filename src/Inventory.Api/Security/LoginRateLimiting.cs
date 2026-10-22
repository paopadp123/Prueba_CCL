using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Inventory.Api.Security;

/// <summary>
/// Límite de intentos de inicio de sesión por dirección IP, contra ataques de fuerza bruta.
/// </summary>
public static class LoginRateLimiting
{
    /// <summary>
    /// Nombre de la política que usa el endpoint de login.
    /// </summary>
    public const string PolicyName = "login";

    /// <summary>
    /// Registra la política con los valores de "RateLimiting:Login" (por defecto 5 intentos por minuto).
    /// </summary>
    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("RateLimiting:Login");
        int permitLimit = section.GetValue("PermitLimit", 5);
        TimeSpan window = TimeSpan.FromSeconds(section.GetValue("WindowSeconds", 60));

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;
            options.AddPolicy(PolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                }));
        });
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        HttpContext httpContext = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        IProblemDetailsService problemDetails = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Demasiados intentos",
                Detail = "Se superó el número de intentos de inicio de sesión. Espere un momento e intente de nuevo.",
                Extensions = { ["code"] = "too_many_requests" },
            },
        });
    }
}
