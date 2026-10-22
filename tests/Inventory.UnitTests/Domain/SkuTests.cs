using Inventory.Domain.Exceptions;
using Inventory.Domain.ValueObjects;

namespace Inventory.UnitTests.Domain;

/// <summary>
/// Pruebas del value object <see cref="Sku"/>.
/// </summary>
public class SkuTests
{
    [Theory]
    [InlineData("LAP-001")]
    [InlineData("ABC")]
    [InlineData("A23456789012345678901234567890")]
    public void Create_WithValidFormat_ReturnsSku(string value)
    {
        Sku sku = Sku.Create(value);

        Assert.Equal(value, sku.Value);
    }

    [Fact]
    public void Create_NormalizesToUpperCaseAndTrims()
    {
        Sku sku = Sku.Create("  lap-001 ");

        Assert.Equal("LAP-001", sku.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB")]
    [InlineData("-AB1")]
    [InlineData("LAP 001")]
    [InlineData("LAP_001")]
    [InlineData("A234567890123456789012345678901")]
    public void Create_WithInvalidFormat_ThrowsInvalidSkuException(string? value)
    {
        Assert.Throws<InvalidSkuException>(() => Sku.Create(value));
    }

    [Fact]
    public void Skus_WithSameValue_AreEqual()
    {
        Assert.Equal(Sku.Create("lap-001"), Sku.Create("LAP-001"));
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        Assert.Equal("LAP-001", Sku.Create("LAP-001").ToString());
    }
}
