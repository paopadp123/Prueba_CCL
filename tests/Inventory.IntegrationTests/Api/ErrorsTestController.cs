using Inventory.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.IntegrationTests.Api;

/// <summary>
/// Endpoint solo para pruebas: lanza excepciones para verificar el manejador global.
/// </summary>
[ApiController]
[Route("test/errors")]
public sealed class ErrorsTestController : ControllerBase
{
    /// <summary>
    /// Lanza la excepción indicada; "ok" responde normalmente.
    /// </summary>
    [HttpGet("{kind}")]
    public IActionResult Throw(string kind) => kind switch
    {
        "ok" => Ok(),
        "stock" => throw new InsufficientStockException(2, 8),
        "not-found" => throw new ProductNotFoundException(7),
        _ => throw new InvalidOperationException("detalle interno que no debe salir"),
    };
}
