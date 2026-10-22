using Inventory.Domain.Exceptions;
using Inventory.Infrastructure.Persistence;
using Npgsql;

namespace Inventory.UnitTests.Infrastructure;

/// <summary>
/// Pruebas de la traducción de errores de PostgreSQL a excepciones de dominio.
/// </summary>
public class PostgresErrorMapperTests
{
    [Fact]
    public void Map_ProductNotFound_ReturnsProductNotFoundException()
    {
        Exception result = PostgresErrorMapper.Map(Error("IN001"), productId: 7);

        ProductNotFoundException exception = Assert.IsType<ProductNotFoundException>(result);
        Assert.Equal(7, exception.ProductId);
    }

    [Fact]
    public void Map_InsufficientStock_ReadsAvailableAndRequestedFromDetail()
    {
        Exception result = PostgresErrorMapper.Map(Error("IN002", detail: "available=2;requested=8"), productId: 7);

        InsufficientStockException exception = Assert.IsType<InsufficientStockException>(result);
        Assert.Equal(2, exception.Available);
        Assert.Equal(8, exception.Requested);
    }

    [Fact]
    public void Map_InactiveProduct_ReturnsInactiveProductException()
    {
        Assert.IsType<InactiveProductException>(PostgresErrorMapper.Map(Error("IN003"), productId: 7));
    }

    [Fact]
    public void Map_UserNotAvailable_ReturnsUserNotAvailableException()
    {
        Exception result = PostgresErrorMapper.Map(Error("IN004"), userId: 3);

        UserNotAvailableException exception = Assert.IsType<UserNotAvailableException>(result);
        Assert.Equal(3, exception.UserId);
    }

    [Fact]
    public void Map_InvalidQuantity_ReturnsInvalidQuantityException()
    {
        Assert.IsType<InvalidQuantityException>(PostgresErrorMapper.Map(Error("IN005"), quantity: 0));
    }

    [Fact]
    public void Map_UniqueViolationOnSku_ReturnsDuplicateSkuException()
    {
        Exception result = PostgresErrorMapper.Map(Error("23505", constraint: "ux_products_sku"), sku: "LAP-001");

        DuplicateSkuException exception = Assert.IsType<DuplicateSkuException>(result);
        Assert.Equal("LAP-001", exception.Sku);
    }

    [Theory]
    [InlineData("23505", "ux_users_username")]
    [InlineData("42P01", null)]
    public void Map_UnknownError_ReturnsOriginalException(string sqlState, string? constraint)
    {
        PostgresException error = Error(sqlState, constraint: constraint);

        Assert.Same(error, PostgresErrorMapper.Map(error));
    }

    private static PostgresException Error(string sqlState, string? detail = null, string? constraint = null) =>
        new("error", "ERROR", "ERROR", sqlState, detail: detail, constraintName: constraint);
}
