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

        HealthResponse body = new(
            report.Status.ToString(),
            Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            [.. report.Entries.Select(entry => new HealthCheckResponse(
                entry.Key,
                entry.Value.Status.ToString(),
                Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
                entry.Value.Description))]);

        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(body, JsonOptions, context.RequestAborted);
    }

    private sealed record HealthResponse(string Status, double TotalDurationMs, IReadOnlyList<HealthCheckResponse> Checks);

    private sealed record HealthCheckResponse(string Name, string Status, double DurationMs, string? Description);
}
