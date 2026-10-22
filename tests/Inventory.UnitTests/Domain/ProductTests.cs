using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.ValueObjects;

namespace Inventory.UnitTests.Domain;

/// <summary>
/// Pruebas de la entidad <see cref="Product"/>.
/// </summary>
public class ProductTests
{
    private static readonly Sku DefaultSku = Sku.Create("LAP-001");

    [Fact]
    public void Create_WithValidData_ReturnsActiveProductWithoutStock()
    {
        Product product = Product.Create(DefaultSku, "  Portátil  ", "  Oficina  ", 3200000m);

        Assert.Equal(DefaultSku, product.Sku);
        Assert.Equal("Portátil", product.Name);
        Assert.Equal("Oficina", product.Description);
        Assert.Equal(3200000m, product.UnitPrice);
        Assert.Equal(0, product.CurrentStock);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Create_WithBlankDescription_StoresNull()
    {
        Product product = Product.Create(DefaultSku, "Portátil", "   ", 10m);

        Assert.Null(product.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainValidationException(string? name)
    {
        Assert.Throws<DomainValidationException>(() => Product.Create(DefaultSku, name, null, 10m));
    }

    [Fact]
    public void Create_WithNameLongerThan150_ThrowsDomainValidationException()
    {
        string name = new('a', 151);

        Assert.Throws<DomainValidationException>(() => Product.Create(DefaultSku, name, null, 10m));
    }

    [Fact]
    public void Create_WithDescriptionLongerThan500_ThrowsDomainValidationException()
    {
        string description = new('a', 501);

        Assert.Throws<DomainValidationException>(() => Product.Create(DefaultSku, "Portátil", description, 10m));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Create_WithNegativePrice_ThrowsDomainValidationException(decimal unitPrice)
    {
        Assert.Throws<DomainValidationException>(() => Product.Create(DefaultSku, "Portátil", null, unitPrice));
    }

    [Fact]
    public void Create_WithPriceAboveColumnLimit_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(
            () => Product.Create(DefaultSku, "Portátil", null, Product.UnitPriceMax + 1m));
    }

    [Fact]
    public void Create_WithMaximumPrice_Succeeds()
    {
        Assert.Equal(Product.UnitPriceMax, Product.Create(DefaultSku, "Portátil", null, Product.UnitPriceMax).UnitPrice);
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimals_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => Product.Create(DefaultSku, "Portátil", null, 10.123m));
    }

    [Fact]
    public void UpdateDetails_WithValidData_ChangesNameDescriptionAndPrice()
    {
        Product product = Product.Create(DefaultSku, "Portátil", null, 10m);

        product.UpdateDetails("Portátil Pro", "Nueva versión", 20m);

        Assert.Equal("Portátil Pro", product.Name);
        Assert.Equal("Nueva versión", product.Description);
        Assert.Equal(20m, product.UnitPrice);
    }

    [Fact]
    public void UpdateDetails_WithInvalidData_KeepsPreviousValues()
    {
        Product product = Product.Create(DefaultSku, "Portátil", null, 10m);

        Assert.Throws<DomainValidationException>(() => product.UpdateDetails("Nuevo", null, -1m));
        Assert.Equal("Portátil", product.Name);
        Assert.Equal(10m, product.UnitPrice);
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        Product product = Product.Create(DefaultSku, "Portátil", null, 10m);

        product.Deactivate();
        Assert.False(product.IsActive);

        product.Activate();
        Assert.True(product.IsActive);
    }

    [Fact]
    public void EnsureCanWithdraw_WithEnoughStock_DoesNotThrow()
    {
        Product product = RehydrateWithStock(10);

        Exception exception = Record.Exception(() => product.EnsureCanWithdraw(Quantity.Create(10)));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureCanWithdraw_WithInsufficientStock_ThrowsWithAvailableAndRequested()
    {
        Product product = RehydrateWithStock(3);

        InsufficientStockException exception = Assert.Throws<InsufficientStockException>(
            () => product.EnsureCanWithdraw(Quantity.Create(5)));

        Assert.Equal(3, exception.Available);
        Assert.Equal(5, exception.Requested);
    }

    [Fact]
    public void EnsureCanWithdraw_WhenInactive_ThrowsInactiveProductException()
    {
        Product product = RehydrateWithStock(10, isActive: false);

        Assert.Throws<InactiveProductException>(() => product.EnsureCanWithdraw(Quantity.Create(1)));
    }

    [Fact]
    public void Rehydrate_RestoresAllState()
    {
        DateTimeOffset createdAt = new(2026, 10, 21, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset updatedAt = createdAt.AddHours(1);

        Product product = Product.Rehydrate(7, "LAP-001", "Portátil", "Oficina", 10m, 4, false, createdAt, updatedAt);

        Assert.Equal(7, product.Id);
        Assert.Equal(DefaultSku, product.Sku);
        Assert.Equal(4, product.CurrentStock);
        Assert.False(product.IsActive);
        Assert.Equal(createdAt, product.CreatedAt);
        Assert.Equal(updatedAt, product.UpdatedAt);
    }

    private static Product RehydrateWithStock(int stock, bool isActive = true) =>
        Product.Rehydrate(1, "LAP-001", "Portátil", null, 10m, stock, isActive, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
}
