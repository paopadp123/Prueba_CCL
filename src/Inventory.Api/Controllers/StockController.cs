using Inventory.Api.Contracts;
using Inventory.Application.Models;
using Inventory.Application.Stock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

/// <summary>
/// Consulta del stock actual. Disponible para Admin y Operator.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public sealed class StockController(StockService stockService) : ControllerBase
{
    /// <summary>
    /// Stock paginado ordenado por nombre, filtrando por SKU o nombre.
    /// </summary>
    [HttpGet("api/stock")]
    [ProducesResponseType<PagedResponse<StockResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<StockResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<StockItem> result = await stockService.ListAsync(
            search, includeInactive, new PageRequest(page, pageSize), cancellationToken);

        return Ok(new PagedResponse<StockResponse>(
            [.. result.Items.Select(StockResponse.From)],
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    /// <summary>
    /// Stock de un producto, aunque esté inactivo.
    /// </summary>
    [HttpGet("api/products/{id:long}/stock")]
    [ProducesResponseType<StockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockResponse>> GetByProduct(long id, CancellationToken cancellationToken) =>
        Ok(StockResponse.From(await stockService.GetByProductAsync(id, cancellationToken)));
}
