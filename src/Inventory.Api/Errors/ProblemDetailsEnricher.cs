using Inventory.Api.Observability;

namespace Inventory.Api.Errors;

/// <summary>
/// Completa todas las respuestas de error con la ruta y el identificador de correlación.
/// </summary>
public static class ProblemDetailsEnricher
{
    /// <summary>
    /// Se usa como <c>ProblemDetailsOptions.CustomizeProblemDetails</c>.
    /// </summary>
    public static void Enrich(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["correlationId"] = CorrelationId.Get(context.HttpContext);
    }
}
