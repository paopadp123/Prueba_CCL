using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.ValueObjects;

namespace Inventory.UnitTests.Domain;

/// <summary>
/// Pruebas de la entidad <see cref="Movement"/>.
/// </summary>
public class MovementTests
{
    [Fact]
    public void Create_WithValidData_ReturnsPendingMovement()
    {
        Movement movement = Movement.Create(3, MovementType.Exit, Quantity.Create(5), 1, "  Venta  ");

        Assert.Equal(0, movement.Id);
        Assert.Equal(3, movement.ProductId);
        Assert.Equal(MovementType.Exit, movement.Type);
        Assert.Equal(5, movement.Quantity.Value);
        Assert.Equal(1, movement.UserId);
        Assert.Equal("Venta", movement.Reason);
        Assert.Null(movement.StockAfter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankReason_StoresNull(string? reason)
    {
        Movement movement = Movement.Create(3, MovementType.Entry, Quantity.Create(1), 1, reason);

        Assert.Null(movement.Reason);
    }

    [Fact]
    public void Create_WithReasonLongerThan250_ThrowsDomainValidationException()
    {
        string reason = new('a', 251);

        Assert.Throws<DomainValidationException>(
            () => Movement.Create(3, MovementType.Entry, Quantity.Create(1), 1, reason));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Create_WithNonPositiveIds_ThrowsDomainValidationException(long productId, long userId)
    {
        Assert.Throws<DomainValidationException>(
            () => Movement.Create(productId, MovementType.Entry, Quantity.Create(1), userId, null));
    }

    [Fact]
    public void Rehydrate_RestoresRegisteredMovement()
    {
        DateTimeOffset createdAt = new(2026, 10, 21, 10, 0, 0, TimeSpan.Zero);

        Movement movement = Movement.Rehydrate(9, 3, MovementType.Entry, 10, 25, "Compra", 1, createdAt);

        Assert.Equal(9, movement.Id);
        Assert.Equal(25, movement.StockAfter);
        Assert.Equal(createdAt, movement.CreatedAt);
    }
}
