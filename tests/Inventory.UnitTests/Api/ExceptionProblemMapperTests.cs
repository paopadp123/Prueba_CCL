using Inventory.Api.Errors;
using Inventory.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Inventory.UnitTests.Api;

/// <summary>
/// Pruebas de la traducción de excepciones a ProblemDetails.
/// </summary>
public class ExceptionProblemMapperTests
{
    public static TheoryData<Exception, int, string> DomainCases => new()
    {
        { new ProductNotFoundException(7), StatusCodes.Status404NotFound, "product_not_found" },
        { new InvalidQuantityException(0), StatusCodes.Status400BadRequest, "invalid_quantity" },
        { new InvalidSkuException("x"), StatusCodes.Status400BadRequest, "invalid_sku" },
        { new DomainValidationException("Nombre requerido"), StatusCodes.Status400BadRequest, "validation_error" },
        { new InsufficientStockException(2, 8), StatusCodes.Status409Conflict, "insufficient_stock" },
        { new DuplicateSkuException("ABC-001"), StatusCodes.Status409Conflict, "duplicate_sku" },
        { new InactiveProductException(7), StatusCodes.Status409Conflict, "inactive_product" },
        { new UserNotAvailableException(3), StatusCodes.Status403Forbidden, "user_not_available" },
    };

    [Theory]
    [MemberData(nameof(DomainCases))]
    public void Map_DomainException_UsesExpectedStatusAndCode(Exception exception, int status, string code)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = ExceptionProblemMapper.Map(exception);

        Assert.Equal(status, problem.Status);
        Assert.Equal(code, problem.Extensions["code"]);
        Assert.Equal(exception.Message, problem.Detail);
    }

    [Fact]
    public void Map_InsufficientStock_IncludesAvailableAndRequested()
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = ExceptionProblemMapper.Map(new InsufficientStockException(2, 8));

        Assert.Equal(2, problem.Extensions["available"]);
        Assert.Equal(8, problem.Extensions["requested"]);
    }

    [Fact]
    public void Map_BadHttpRequest_KeepsItsStatusCode()
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = ExceptionProblemMapper.Map(new BadHttpRequestException("JSON inválido", StatusCodes.Status400BadRequest));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("bad_request", problem.Extensions["code"]);
    }

    [Fact]
    public void Map_UnexpectedException_ReturnsGenericErrorWithoutInternalDetails()
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = ExceptionProblemMapper.Map(new InvalidOperationException("cadena de conexión secreta"));

        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal(ExceptionProblemMapper.UnexpectedErrorCode, problem.Extensions["code"]);
        Assert.DoesNotContain("secreta", problem.Detail, StringComparison.Ordinal);
    }
}
