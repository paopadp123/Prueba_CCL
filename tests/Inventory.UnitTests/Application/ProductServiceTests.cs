using Inventory.Application.Abstractions;
using Inventory.Application.Models;
using Inventory.Application.Products;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;

namespace Inventory.UnitTests.Application;

/// <summary>
/// Pruebas de los casos de uso del catálogo de productos.
/// </summary>
public class ProductServiceTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    private readonly InMemoryProducts products = new();
    private readonly ProductService service;

    public ProductServiceTests()
    {
        service = new ProductService(products);
    }

    [Fact]
    public async Task CreateAsync_ValidCommand_SavesNormalizedProduct()
    {
        Product product = await service.CreateAsync(new CreateProductCommand(" lap-001 ", " Portátil ", null, 100m), None);

        Assert.True(product.Id > 0);
        Assert.Equal("LAP-001", product.Sku.Value);
        Assert.Equal("Portátil", product.Name);
        Assert.True(product.IsActive);
        Assert.Equal(0, product.CurrentStock);
    }

    [Fact]
    public async Task CreateAsync_InvalidSku_ThrowsWithoutSaving()
    {
        await Assert.ThrowsAsync<InvalidSkuException>(
            () => service.CreateAsync(new CreateProductCommand("x", "Portátil", null, 100m), None));

        Assert.Empty(products.Saved);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ThrowsProductNotFound()
    {
        ProductNotFoundException exception = await Assert.ThrowsAsync<ProductNotFoundException>(
            () => service.GetAsync(99, None));

        Assert.Equal(99, exception.ProductId);
    }

    [Fact]
    public async Task UpdateAsync_ChangesDetailsAndKeepsSku()
    {
        Product created = await service.CreateAsync(new CreateProductCommand("LAP-001", "Portátil", null, 100m), None);

        Product updated = await service.UpdateAsync(
            created.Id, new UpdateProductCommand("Portátil 14\"", "Oficina", 120.5m), None);

        Assert.Equal("LAP-001", updated.Sku.Value);
        Assert.Equal("Portátil 14\"", updated.Name);
        Assert.Equal("Oficina", updated.Description);
        Assert.Equal(120.5m, updated.UnitPrice);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsProductNotFound()
    {
        await Assert.ThrowsAsync<ProductNotFoundException>(
            () => service.UpdateAsync(99, new UpdateProductCommand("Nombre", null, 1m), None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetActiveAsync_ChangesStatus(bool isActive)
    {
        Product created = await service.CreateAsync(new CreateProductCommand("LAP-001", "Portátil", null, 100m), None);

        Product updated = await service.SetActiveAsync(created.Id, isActive, None);

        Assert.Equal(isActive, updated.IsActive);
    }

    private sealed class InMemoryProducts : IProductRepository
    {
        private long nextId = 1;

        public Dictionary<long, Product> Saved { get; } = [];

        public Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Saved.GetValueOrDefault(id));

        public Task<PagedResult<Product>> ListAsync(
            string? search, bool includeInactive, PageRequest page, CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<Product>([.. Saved.Values], page.Page, page.PageSize, Saved.Count));

        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken)
        {
            Product saved = Product.Rehydrate(
                nextId++, product.Sku.Value, product.Name, product.Description, product.UnitPrice,
                0, product.IsActive, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
            Saved[saved.Id] = saved;
            return Task.FromResult(saved);
        }

        public Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken)
        {
            Saved[product.Id] = product;
            return Task.FromResult(product);
        }
    }
}
