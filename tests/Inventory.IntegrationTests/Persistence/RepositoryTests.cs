using Inventory.Application.Abstractions;
using Inventory.Application.Models;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.IntegrationTests.Persistence;

/// <summary>
/// Pruebas de repositorios y consultas contra PostgreSQL real.
/// Cada prueba usa SKUs propios para no depender de las demás.
/// </summary>
[Collection(DatabaseTestGroup.Name)]
public sealed class RepositoryTests(DatabaseFixture database)
{
    private static readonly CancellationToken None = CancellationToken.None;

    [Fact]
    public async Task AddAsync_PersistsProductAndGetByIdReturnsIt()
    {
        await using ServiceProvider services = database.CreateServices(database.AdminUserId);
        IProductRepository products = services.GetRequiredService<IProductRepository>();

        Product saved = await products.AddAsync(NewProduct("IT-ADD-001"), None);
        Product? loaded = await products.GetByIdAsync(saved.Id, None);

        Assert.True(saved.Id > 0);
        Assert.NotNull(loaded);
        Assert.Equal("IT-ADD-001", loaded.Sku.Value);
        Assert.Equal(0, loaded.CurrentStock);
        Assert.True(loaded.IsActive);
    }

    [Fact]
    public async Task AddAsync_RecordsCurrentUserInAudit()
    {
        await using ServiceProvider services = database.CreateServices(database.AdminUserId);
        Product saved = await services.GetRequiredService<IProductRepository>().AddAsync(NewProduct("IT-AUD-001"), None);

        long auditUser = await database.ScalarAsync<long>(
            "SELECT app_user_id FROM inventory.audit_log WHERE table_name = 'products' AND record_id = @id AND operation = 'INSERT'",
            ("id", saved.Id));

        Assert.Equal(database.AdminUserId, auditUser);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateSku_ThrowsDuplicateSkuException()
    {
        await using ServiceProvider services = database.CreateServices();
        IProductRepository products = services.GetRequiredService<IProductRepository>();
        await products.AddAsync(NewProduct("IT-DUP-001"), None);

        DuplicateSkuException exception = await Assert.ThrowsAsync<DuplicateSkuException>(
            () => products.AddAsync(NewProduct("IT-DUP-001"), None));

        Assert.Equal("IT-DUP-001", exception.Sku);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        await using ServiceProvider services = database.CreateServices();
        IProductRepository products = services.GetRequiredService<IProductRepository>();
        Product product = await products.AddAsync(NewProduct("IT-UPD-001"), None);

        product.UpdateDetails("Nombre nuevo", "Descripción nueva", 99.5m);
        product.Deactivate();
        await products.UpdateAsync(product, None);
        Product? loaded = await products.GetByIdAsync(product.Id, None);

        Assert.NotNull(loaded);
        Assert.Equal("Nombre nuevo", loaded.Name);
        Assert.Equal(99.5m, loaded.UnitPrice);
        Assert.False(loaded.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductDoesNotExist_ThrowsProductNotFoundException()
    {
        await using ServiceProvider services = database.CreateServices();
        Product missing = Product.Rehydrate(
            999_999, "IT-MISSING", "No existe", null, 1m, 0, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<ProductNotFoundException>(
            () => services.GetRequiredService<IProductRepository>().UpdateAsync(missing, None));
    }

    [Fact]
    public async Task RegisterAsync_EntryAndExit_UpdateStock()
    {
        await using ServiceProvider services = database.CreateServices();
        Product product = await services.GetRequiredService<IProductRepository>().AddAsync(NewProduct("IT-MOV-001"), None);
        IMovementRepository movements = services.GetRequiredService<IMovementRepository>();

        RegisteredMovement entry = await movements.RegisterAsync(Movement(product.Id, MovementType.Entry, 10), None);
        RegisteredMovement exit = await movements.RegisterAsync(Movement(product.Id, MovementType.Exit, 4), None);

        Assert.True(entry.MovementId > 0);
        Assert.Equal(10, entry.StockAfter);
        Assert.Equal(6, exit.StockAfter);
    }

    [Fact]
    public async Task RegisterAsync_ExitAboveStock_ThrowsInsufficientStockException()
    {
        await using ServiceProvider services = database.CreateServices();
        Product product = await services.GetRequiredService<IProductRepository>().AddAsync(NewProduct("IT-MOV-002"), None);
        IMovementRepository movements = services.GetRequiredService<IMovementRepository>();
        await movements.RegisterAsync(Movement(product.Id, MovementType.Entry, 2), None);

        InsufficientStockException exception = await Assert.ThrowsAsync<InsufficientStockException>(
            () => movements.RegisterAsync(Movement(product.Id, MovementType.Exit, 8), None));

        Assert.Equal(2, exception.Available);
        Assert.Equal(8, exception.Requested);
    }

    [Fact]
    public async Task RegisterAsync_UnknownProduct_ThrowsProductNotFoundException()
    {
        await using ServiceProvider services = database.CreateServices();

        ProductNotFoundException exception = await Assert.ThrowsAsync<ProductNotFoundException>(
            () => services.GetRequiredService<IMovementRepository>()
                .RegisterAsync(Movement(999_999, MovementType.Entry, 1), None));

        Assert.Equal(999_999, exception.ProductId);
    }

    [Fact]
    public async Task RegisterAsync_InactiveProduct_ThrowsInactiveProductException()
    {
        await using ServiceProvider services = database.CreateServices();
        IProductRepository products = services.GetRequiredService<IProductRepository>();
        Product product = await products.AddAsync(NewProduct("IT-MOV-003"), None);
        product.Deactivate();
        await products.UpdateAsync(product, None);

        await Assert.ThrowsAsync<InactiveProductException>(
            () => services.GetRequiredService<IMovementRepository>()
                .RegisterAsync(Movement(product.Id, MovementType.Entry, 1), None));
    }

    [Fact]
    public async Task RegisterAsync_UnknownUser_ThrowsUserNotAvailableException()
    {
        await using ServiceProvider services = database.CreateServices();
        Product product = await services.GetRequiredService<IProductRepository>().AddAsync(NewProduct("IT-MOV-004"), None);

        await Assert.ThrowsAsync<UserNotAvailableException>(
            () => services.GetRequiredService<IMovementRepository>().RegisterAsync(
                Domain.Entities.Movement.Create(product.Id, MovementType.Entry, Quantity.Create(1), 999_999, null),
                None));
    }

    [Fact]
    public async Task GetStockAsync_FiltersAndPaginates()
    {
        await using ServiceProvider services = database.CreateServices();
        IProductRepository products = services.GetRequiredService<IProductRepository>();
        await products.AddAsync(NewProduct("IT-STK-001"), None);
        await products.AddAsync(NewProduct("IT-STK-002"), None);
        await products.AddAsync(NewProduct("IT-STK-003"), None);

        PagedResult<StockItem> page = await services.GetRequiredService<IInventoryQueries>()
            .GetStockAsync("it-stk", onlyActive: true, new PageRequest(2, 2), None);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task GetMovementsAsync_ReturnsProductHistoryNewestFirst()
    {
        await using ServiceProvider services = database.CreateServices();
        Product product = await services.GetRequiredService<IProductRepository>().AddAsync(NewProduct("IT-HIS-001"), None);
        IMovementRepository movements = services.GetRequiredService<IMovementRepository>();
        await movements.RegisterAsync(Movement(product.Id, MovementType.Entry, 5), None);
        await movements.RegisterAsync(Movement(product.Id, MovementType.Exit, 2), None);

        PagedResult<MovementHistoryItem> history = await services.GetRequiredService<IInventoryQueries>()
            .GetMovementsAsync(product.Id, new PageRequest(1, 10), None);

        Assert.Equal(2, history.TotalCount);
        Assert.Equal(MovementType.Exit, history.Items[0].Type);
        Assert.Equal(3, history.Items[0].StockAfter);
        Assert.Equal("admin", history.Items[0].Username);
    }

    private static Product NewProduct(string sku) =>
        Product.Create(Sku.Create(sku), $"Producto {sku}", null, 1000m);

    private Movement Movement(long productId, MovementType type, int quantity) =>
        Domain.Entities.Movement.Create(productId, type, Quantity.Create(quantity), database.AdminUserId, "Prueba");
}
