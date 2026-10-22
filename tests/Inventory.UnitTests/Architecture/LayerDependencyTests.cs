using System.Reflection;

namespace Inventory.UnitTests.Architecture;

/// <summary>
/// Verifica que las dependencias entre capas respeten Clean Architecture.
/// </summary>
public class LayerDependencyTests
{
    private const string Domain = "Inventory.Domain";
    private const string Application = "Inventory.Application";
    private const string Infrastructure = "Inventory.Infrastructure";
    private const string Api = "Inventory.Api";

    [Theory]
    [InlineData(Domain, Application)]
    [InlineData(Domain, Infrastructure)]
    [InlineData(Domain, Api)]
    [InlineData(Application, Infrastructure)]
    [InlineData(Application, Api)]
    public void Layer_ShouldNotDependOn_OuterLayer(string layer, string forbiddenDependency)
    {
        IEnumerable<string?> references = Assembly.Load(layer)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name);

        Assert.DoesNotContain(forbiddenDependency, references);
    }
}
