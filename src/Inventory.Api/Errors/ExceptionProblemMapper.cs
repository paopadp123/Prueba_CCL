using Inventory.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Errors;

/// <summary>
/// Traduce excepciones a respuestas ProblemDetails (RFC 9457).
/// Las reglas de negocio conservan su código estable; los errores inesperados
/// devuelven un mensaje genérico para no filtrar detalles internos.
/// </summary>
public static class ExceptionProblemMapper
{
    /// <summary>
    /// Código que se devuelve para errores no controlados.
    /// </summary>
    public const string UnexpectedErrorCode = "unexpected_error";

    /// <summary>
    /// Construye el ProblemDetails correspondiente a la excepción.
    /// </summary>
    public static ProblemDetails Map(Exception exception) => exception switch
    {
        ProductNotFoundException e => Problem(StatusCodes.Status404NotFound, "Recurso no encontrado", e),
        DomainValidationException e => Problem(StatusCodes.Status400BadRequest, "Datos inválidos", e),
        InsufficientStockException e => WithValues(
            Problem(StatusCodes.Status409Conflict, "Stock insuficiente", e),
            ("available", e.Available),
            ("requested", e.Requested)),
        DuplicateSkuException e => Problem(StatusCodes.Status409Conflict, "SKU duplicado", e),
        InactiveProductException e => Problem(StatusCodes.Status409Conflict, "Producto inactivo", e),
        UserNotAvailableException e => Problem(StatusCodes.Status403Forbidden, "Usuario no habilitado", e),
        DomainException e => Problem(StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida", e),
        BadHttpRequestException e => Problem(e.StatusCode, "Solicitud inválida", "bad_request", "La solicitud no tiene un formato válido."),
        _ => Problem(
            StatusCodes.Status500InternalServerError,
            "Error interno",
            UnexpectedErrorCode,
            "Ocurrió un error inesperado. Si persiste, repórtelo indicando el correlationId."),
    };

    private static ProblemDetails Problem(int status, string title, DomainException exception) =>
        Problem(status, title, exception.Code, exception.Message);

    private static ProblemDetails Problem(int status, string title, string code, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Extensions = { ["code"] = code },
    };

    private static ProblemDetails WithValues(ProblemDetails problem, params (string Key, object Value)[] values)
    {
        foreach ((string key, object value) in values)
        {
            problem.Extensions[key] = value;
        }

        return problem;
    }
}
