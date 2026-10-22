using System.Security.Claims;
using Inventory.Api.Security;

namespace Inventory.UnitTests.Api;

/// <summary>
/// Pruebas de la lectura del usuario autenticado desde los claims.
/// </summary>
public class HttpUserContextTests
{
    [Theory]
    [InlineData(HttpUserContext.SubjectClaim)]
    [InlineData(ClaimTypes.NameIdentifier)]
    public void ReadUserId_ValidClaim_ReturnsId(string claimType)
    {
        Assert.Equal(42, HttpUserContext.ReadUserId(Principal(claimType, "42")));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    public void ReadUserId_InvalidValue_ReturnsNull(string value)
    {
        Assert.Null(HttpUserContext.ReadUserId(Principal(HttpUserContext.SubjectClaim, value)));
    }

    [Fact]
    public void ReadUserId_Anonymous_ReturnsNull()
    {
        Assert.Null(HttpUserContext.ReadUserId(new ClaimsPrincipal(new ClaimsIdentity())));
        Assert.Null(HttpUserContext.ReadUserId(null));
    }

    private static ClaimsPrincipal Principal(string type, string value) =>
        new(new ClaimsIdentity([new Claim(type, value)], "test"));
}
