using Inventory.Application.Abstractions;
using Inventory.Application.Models;
using Inventory.Application.Stock;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;

namespace Inventory.UnitTests.Application;

/// <summary>
/// Pruebas de las consultas de stock.
/// </summary>
public class StockServiceTests
{
    private static readonly CancellationToken None = CancellationToken.None;
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeQueries queries = new();
    private readonly FakeProducts products = new();

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ListAsync_TranslatesIncludeInactiveToOnlyActive(bool includeInactive, bool expectedOnlyActive)
    {
        await Service().ListAsync("lap", includeInactive, new PageRequest(2, 10), None);

        Assert.Equal("lap", queries.Search);
        Assert.Equal(expectedOnlyActive, queries.OnlyActive);
        Assert.Equal(2, queries.Page?.Page);
    }

    [Fact]
    public async Task GetByProductAsync_ReturnsStockAndValue()
    {
        products.Stored = Product.Rehydrate(7, "LAP-001", "Portátil", null, 150.5m, 4, false, Now, Now);

        StockItem stock = await Service().GetByProductAsync(7, None);

        Assert.Equal(7, stock.ProductId);
        Assert.Equal("LAP-001", stock.Sku);
        Assert.Equal(4, stock.CurrentStock);
        Assert.Equal(602m, stock.StockValue);
        Assert.False(stock.IsActive);
        Assert.Equal(Now, stock.UpdatedAt);
    }

    [Fact]
    public async Task GetByProductAsync_UnknownProduct_ThrowsProductNotFound()
    {
        ProductNotFoundException exception = await Assert.ThrowsAsync<ProductNotFoundException>(
            () => Service().GetByProductAsync(99, None));

        Assert.Equal(99, exception.ProductId);
    }

    private StockService Service() => new(queries, products);

    private sealed class FakeQueries : IInventoryQueries
    {
        public string? Search { get; private set; }

        public bool? OnlyActive { get; private set; }

        public PageRequest? Page { get; private set; }

        public Task<PagedResult<StockItem>> GetStockAsync(
            string? search, bool onlyActive, PageRequest page, CancellationToken cancellationToken)
        {
            Search = search;
            OnlyActive = onlyActive;
            Page = page;
            return Task.FromResult(new PagedResult<StockItem>([], page.Page, page.PageSize, 0));
        }

        public Task<PagedResult<MovementHistoryItem>> GetMovementsAsync(
            long? productId, PageRequest page, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeProducts : IProductRepository
    {
        public Product? Stored { get; set; }

        public Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Stored?.Id == id ? Stored : null);

        public Task<PagedResult<Product>> ListAsync(
            string? search, bool includeInactive, PageRequest page, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
