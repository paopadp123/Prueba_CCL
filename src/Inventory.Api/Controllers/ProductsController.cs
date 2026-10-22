using Inventory.Api.Contracts;
using Inventory.Application.Auth;
using Inventory.Application.Models;
using Inventory.Application.Products;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

/// <summary>
/// Catálogo de productos. Cualquier usuario autenticado consulta; solo Admin modifica.
/// No hay borrado físico: desactivar es la baja lógica y conserva el historial de movimientos.
/// </summary>
[ApiController]
[Authorize]
[Route("api/products")]
[Produces("application/json")]
public sealed class ProductsController(ProductService productService) : ControllerBase
{
    /// <summary>
    /// Lista productos paginados, filtrando por SKU o nombre.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<Product> result = await productService.ListAsync(
            search, includeInactive, new PageRequest(page, pageSize), cancellationToken);

        return Ok(new PagedResponse<ProductResponse>(
            [.. result.Items.Select(ProductResponse.From)],
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    /// <summary>
    /// Obtiene un producto por su identificador.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Get(long id, CancellationToken cancellationToken) =>
        Ok(ProductResponse.From(await productService.GetAsync(id, cancellationToken)));

    /// <summary>
    /// Crea un producto activo y sin stock.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Product product = await productService.CreateAsync(
            new CreateProductCommand(request.Sku, request.Name, request.Description, request.UnitPrice!.Value),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = product.Id }, ProductResponse.From(product));
    }

    /// <summary>
    /// Actualiza nombre, descripción y precio.
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Update(
        long id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Product product = await productService.UpdateAsync(
            id,
            new UpdateProductCommand(request.Name, request.Description, request.UnitPrice!.Value),
            cancellationToken);

        return Ok(ProductResponse.From(product));
    }

    /// <summary>
    /// Activa o desactiva el producto (baja lógica).
    /// </summary>
    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> ChangeStatus(
        long id, ChangeProductStatusRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Product product = await productService.SetActiveAsync(id, request.IsActive!.Value, cancellationToken);
        return Ok(ProductResponse.From(product));
    }
}
