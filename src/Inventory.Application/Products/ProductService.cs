using Inventory.Application.Abstractions;
using Inventory.Application.Models;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.ValueObjects;

namespace Inventory.Application.Products;

/// <summary>
/// Casos de uso del catálogo de productos. Las reglas de cada dato las valida el dominio;
/// la unicidad del SKU la garantiza la base de datos.
/// </summary>
public sealed class ProductService(IProductRepository products)
{
    /// <summary>
    /// Lista productos paginados, filtrando por SKU o nombre.
    /// </summary>
    public Task<PagedResult<Product>> ListAsync(
        string? search, bool includeInactive, PageRequest page, CancellationToken cancellationToken) =>
        products.ListAsync(search, includeInactive, page, cancellationToken);

    /// <summary>
    /// Obtiene un producto o lanza <see cref="ProductNotFoundException"/>.
    /// </summary>
    public async Task<Product> GetAsync(long id, CancellationToken cancellationToken) =>
        await products.GetByIdAsync(id, cancellationToken) ?? throw new ProductNotFoundException(id);

    /// <summary>
    /// Crea un producto activo y sin stock; el stock solo entra con movimientos.
    /// </summary>
    public Task<Product> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Product product = Product.Create(Sku.Create(command.Sku), command.Name, command.Description, command.UnitPrice);
        return products.AddAsync(product, cancellationToken);
    }

    /// <summary>
    /// Actualiza nombre, descripción y precio.
    /// </summary>
    public async Task<Product> UpdateAsync(long id, UpdateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Product product = await GetAsync(id, cancellationToken);
        product.UpdateDetails(command.Name, command.Description, command.UnitPrice);
        return await products.UpdateAsync(product, cancellationToken);
    }

    /// <summary>
    /// Activa o desactiva el producto. Desactivar es la baja lógica: conserva el historial.
    /// </summary>
    public async Task<Product> SetActiveAsync(long id, bool isActive, CancellationToken cancellationToken)
    {
        Product product = await GetAsync(id, cancellationToken);
        if (isActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        return await products.UpdateAsync(product, cancellationToken);
    }
}
