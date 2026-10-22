using Inventory.Api.Contracts;
using Inventory.Application.Models;
using Inventory.Application.Movements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

/// <summary>
/// Entradas, salidas e historial de movimientos. Disponible para Admin y Operator.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public sealed class MovementsController(MovementService movementService) : ControllerBase
{
    /// <summary>
    /// Registra una entrada de stock.
    /// </summary>
    [HttpPost("api/movements/entries")]
    [ProducesResponseType<RegisteredMovementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisteredMovementResponse>> RegisterEntry(
        RegisterMovementRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        RegisteredMovement movement = await movementService.RegisterEntryAsync(
            request.ProductId!.Value, request.Quantity!.Value, request.Reason, cancellationToken);

        return CreatedAtAction(nameof(ListByProduct), new { id = movement.ProductId }, RegisteredMovementResponse.From(movement));
    }

    /// <summary>
    /// Registra una salida de stock; responde 409 si el stock no alcanza.
    /// </summary>
    [HttpPost("api/movements/exits")]
    [ProducesResponseType<RegisteredMovementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisteredMovementResponse>> RegisterExit(
        RegisterMovementRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        RegisteredMovement movement = await movementService.RegisterExitAsync(
            request.ProductId!.Value, request.Quantity!.Value, request.Reason, cancellationToken);

        return CreatedAtAction(nameof(ListByProduct), new { id = movement.ProductId }, RegisteredMovementResponse.From(movement));
    }

    /// <summary>
    /// Historial de todos los movimientos, del más reciente al más antiguo.
    /// </summary>
    [HttpGet("api/movements")]
    [ProducesResponseType<PagedResponse<MovementResponse>>(StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResponse<MovementResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        HistoryAsync(null, page, pageSize, cancellationToken);

    /// <summary>
    /// Historial de movimientos de un producto.
    /// </summary>
    [HttpGet("api/products/{id:long}/movements")]
    [ProducesResponseType<PagedResponse<MovementResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<PagedResponse<MovementResponse>>> ListByProduct(
        long id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        HistoryAsync(id, page, pageSize, cancellationToken);

    private async Task<ActionResult<PagedResponse<MovementResponse>>> HistoryAsync(
        long? productId, int page, int pageSize, CancellationToken cancellationToken)
    {
        PagedResult<MovementHistoryItem> result = await movementService.GetHistoryAsync(
            productId, new PageRequest(page, pageSize), cancellationToken);

        return Ok(new PagedResponse<MovementResponse>(
            [.. result.Items.Select(MovementResponse.From)],
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }
}
