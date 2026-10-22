using Serilog.Context;

namespace Inventory.Api.Observability;

/// <summary>
/// Asigna un identificador de correlación a cada solicitud, lo agrega a todos los logs
/// emitidos durante ella y lo devuelve en la respuesta.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Procesa la solicitud.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string correlationId = CorrelationId.Resolve(context.Request.Headers[CorrelationId.HeaderName]);
        context.TraceIdentifier = correlationId;

        // OnStarting: el manejador de errores limpia los encabezados antes de escribir la respuesta.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationId.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(CorrelationId.LogProperty, correlationId))
        {
            await next(context);
        }
    }
}
