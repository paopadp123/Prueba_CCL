using Inventory.Domain.Exceptions;
using Inventory.Domain.ValueObjects;

namespace Inventory.UnitTests.Domain;

/// <summary>
/// Pruebas del value object <see cref="Quantity"/>.
/// </summary>
public class QuantityTests
{
    [Fact]
    public void Create_WithPositiveValue_ReturnsQuantity()
    {
        var quantity = Quantity.Create(5);

        Assert.Equal(5, quantity.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Create_WithZeroOrNegativeValue_ThrowsInvalidQuantityException(int value)
    {
        Assert.Throws<InvalidQuantityException>(() => Quantity.Create(value));
    }

    [Fact]
    public void Quantities_WithSameValue_AreEqual()
    {
        Assert.Equal(Quantity.Create(5), Quantity.Create(5));
    }
}
