using Inventory.Api.Observability;

namespace Inventory.UnitTests.Api;

/// <summary>
/// Pruebas de las reglas del identificador de correlación.
/// </summary>
public class CorrelationIdTests
{
    [Theory]
    [InlineData("abc-123")]
    [InlineData("0f8fad5b.d9cb_469f")]
    public void Resolve_ValidIncomingValue_KeepsIt(string incoming)
    {
        Assert.Equal(incoming, CorrelationId.Resolve(incoming));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tiene espacios")]
    [InlineData("salto\nde-linea")]
    public void Resolve_MissingOrUnsafeValue_GeneratesNewId(string? incoming)
    {
        string result = CorrelationId.Resolve(incoming);

        Assert.NotEqual(incoming, result);
        Assert.Equal(32, result.Length);
    }

    [Fact]
    public void Resolve_TooLongValue_GeneratesNewId()
    {
        string incoming = new('a', 65);

        Assert.NotEqual(incoming, CorrelationId.Resolve(incoming));
    }
}
