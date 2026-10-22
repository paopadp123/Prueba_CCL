using Microsoft.AspNetCore.Diagnostics;

namespace Inventory.Api.Errors;

/// <summary>
/// Manejador global de excepciones: registra el error y responde con ProblemDetails.
/// </summary>
public sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = ExceptionProblemMapper.Map(exception);
        var status = problem.Status ?? StatusCodes.Status500InternalServerError;

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnexpectedError(logger, exception);
        }
        else
        {
            LogHandledError(logger, status, problem.Extensions["code"], exception.Message);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error no controlado al procesar la solicitud")]
    private static partial void LogUnexpectedError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Solicitud rechazada con {Status} ({Code}): {Reason}")]
    private static partial void LogHandledError(ILogger logger, int status, object? code, string reason);
}
