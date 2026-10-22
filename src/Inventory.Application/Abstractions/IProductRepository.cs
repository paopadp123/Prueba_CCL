using Inventory.Domain.Entities;

namespace Inventory.Application.Abstractions;

/// <summary>
/// Persistencia de productos.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Obtiene un producto por su identificador, o nulo si no existe.
    /// </summary>
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Guarda un producto nuevo y lo devuelve con su identificador y fechas.
    /// Lanza DuplicateSkuException si el SKU ya existe.
    /// </summary>
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken);

    /// <summary>
    /// Guarda los cambios de un producto existente y lo devuelve actualizado.
    /// Lanza ProductNotFoundException si no existe.
    /// </summary>
    Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken);
}
