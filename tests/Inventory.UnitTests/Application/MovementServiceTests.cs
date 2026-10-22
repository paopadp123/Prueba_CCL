using Inventory.Application.Abstractions;
using Inventory.Application.Models;
using Inventory.Application.Movements;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;

namespace Inventory.UnitTests.Application;

/// <summary>
/// Pruebas de los casos de uso de movimientos.
/// </summary>
public class MovementServiceTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    private readonly FakeMovements movements = new();
    private readonly FakeProducts products = new();
    private readonly FakeQueries queries = new();
    private readonly FakeUser user = new() { UserId = 5 };

    [Fact]
    public async Task RegisterEntryAsync_SendsEntryWithCurrentUser()
    {
        RegisteredMovement result = await Service().RegisterEntryAsync(3, 10, "  Compra  ", None);

        Movement sent = Assert.Single(movements.Registered);
        Assert.Equal(MovementType.Entry, sent.Type);
        Assert.Equal(5, sent.UserId);
        Assert.Equal(10, sent.Quantity.Value);
        Assert.Equal("Compra", sent.Reason);
        Assert.Equal(MovementType.Entry, result.Type);
    }

    [Fact]
    public async Task RegisterExitAsync_SendsExit()
    {
        await Service().RegisterExitAsync(3, 2, null, None);

        Assert.Equal(MovementType.Exit, Assert.Single(movements.Registered).Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task RegisterExitAsync_InvalidQuantity_ThrowsWithoutCallingDatabase(int quantity)
    {
        await Assert.ThrowsAsync<InvalidQuantityException>(() => Service().RegisterExitAsync(3, quantity, null, None));

        Assert.Empty(movements.Registered);
    }

    [Fact]
    public async Task RegisterEntryAsync_WithoutAuthenticatedUser_Throws()
    {
        user.UserId = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service().RegisterEntryAsync(3, 1, null, None));
        Assert.Empty(movements.Registered);
    }

    [Fact]
    public async Task GetHistoryAsync_UnknownProduct_ThrowsProductNotFound()
    {
        await Assert.ThrowsAsync<ProductNotFoundException>(
            () => Service().GetHistoryAsync(99, new PageRequest(1, 10), None));
    }

    [Fact]
    public async Task GetHistoryAsync_WithoutProduct_ReturnsAllMovements()
    {
        PagedResult<MovementHistoryItem> result = await Service().GetHistoryAsync(null, new PageRequest(1, 10), None);

        Assert.Null(queries.RequestedProductId);
        Assert.Equal(0, result.TotalCount);
    }

    private MovementService Service() => new(movements, products, queries, user);

    private sealed class FakeMovements : IMovementRepository
    {
        public List<Movement> Registered { get; } = [];

        public Task<RegisteredMovement> RegisterAsync(Movement movement, CancellationToken cancellationToken)
        {
            Registered.Add(movement);
            return Task.FromResult(new RegisteredMovement(1, movement.ProductId, movement.Type, movement.Quantity.Value, 10));
        }
    }

    private sealed class FakeProducts : IProductRepository
    {
        public Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult<Product?>(null);

        public Task<PagedResult<Product>> ListAsync(
            string? search, bool includeInactive, PageRequest page, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeQueries : IInventoryQueries
    {
        public long? RequestedProductId { get; private set; } = -1;

        public Task<PagedResult<StockItem>> GetStockAsync(
            string? search, bool onlyActive, PageRequest page, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PagedResult<MovementHistoryItem>> GetMovementsAsync(
            long? productId, PageRequest page, CancellationToken cancellationToken)
        {
            RequestedProductId = productId;
            return Task.FromResult(new PagedResult<MovementHistoryItem>([], page.Page, page.PageSize, 0));
        }
    }

    private sealed class FakeUser : IUserContext
    {
        public long? UserId { get; set; }
    }
}
