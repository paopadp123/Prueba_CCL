using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Inventory.Api.Health;

/// <summary>
/// Escribe el resultado de los health checks en JSON, sin exponer mensajes de excepción.
/// </summary>
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Se usa como <c>HealthCheckOptions.ResponseWriter</c>.
    /// </summary>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        var body = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
                description = entry.Value.Description,
            }),
        };

        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(body, JsonOptions, context.RequestAborted);
    }
}
