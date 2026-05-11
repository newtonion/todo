using System.Text.Json;
using Api.Domain.Exceptions;
using Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Api.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Theory]
    [InlineData(typeof(ValidationException), StatusCodes.Status400BadRequest, "Validation error")]
    [InlineData(typeof(NotFoundException), StatusCodes.Status404NotFound, "Resource not found")]
    [InlineData(typeof(UnauthorizedAccessException), StatusCodes.Status401Unauthorized, "Unauthorized")]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status500InternalServerError, "An unexpected error occurred")]
    public async Task InvokeAsync_WhenExceptionIsThrown_WritesExpectedErrorResponse(
        Type exceptionType,
        int expectedStatusCode,
        string expectedTitle)
    {
        var context = CreateHttpContext();
        context.TraceIdentifier = "trace-123";
        context.Items[CorrelationMiddleware.HeaderName] = "correlation-123";
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw CreateException(exceptionType),
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            CreateEnvironment("Production"));

        await middleware.InvokeAsync(context);

        var response = await ReadJsonResponseAsync(context);
        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.Equal(expectedStatusCode, response.RootElement.GetProperty("Status").GetInt32());
        Assert.Equal(expectedTitle, response.RootElement.GetProperty("Title").GetString());
        Assert.Equal("trace-123", response.RootElement.GetProperty("TraceId").GetString());
        Assert.Equal("correlation-123", response.RootElement.GetProperty("CorrelationId").GetString());
        Assert.Equal(JsonValueKind.Null, response.RootElement.GetProperty("Debug").ValueKind);
    }

    [Fact]
    public async Task InvokeAsync_WhenEnvironmentIsDevelopmentAndUnexpectedError_IncludesDebugDetails()
    {
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Something broke"),
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            CreateEnvironment("Development"));

        await middleware.InvokeAsync(context);

        var response = await ReadJsonResponseAsync(context);
        Assert.Contains("Something broke", response.RootElement.GetProperty("Debug").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenEnvironmentIsDevelopmentAndExpectedError_DoesNotIncludeDebugDetails()
    {
        var context = CreateHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("List not found"),
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            CreateEnvironment("Development"));

        await middleware.InvokeAsync(context);

        var response = await ReadJsonResponseAsync(context);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal(JsonValueKind.Null, response.RootElement.GetProperty("Debug").ValueKind);
    }

    [Fact]
    public async Task InvokeAsync_WhenCorrelationIdIsMissing_UsesTraceIdentifier()
    {
        var context = CreateHttpContext();
        context.TraceIdentifier = "trace-456";
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException(),
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            CreateEnvironment("Production"));

        await middleware.InvokeAsync(context);

        var response = await ReadJsonResponseAsync(context);
        Assert.Equal("trace-456", response.RootElement.GetProperty("CorrelationId").GetString());
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };
    }

    private static Exception CreateException(Type exceptionType)
    {
        if (exceptionType == typeof(ValidationException))
        {
            return new ValidationException();
        }

        if (exceptionType == typeof(NotFoundException))
        {
            return new NotFoundException();
        }

        if (exceptionType == typeof(UnauthorizedAccessException))
        {
            return new UnauthorizedAccessException();
        }

        return new InvalidOperationException();
    }

    private static IHostEnvironment CreateEnvironment(string environmentName)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);
        environment.SetupGet(e => e.ApplicationName).Returns("Api.Tests");
        environment.SetupGet(e => e.ContentRootPath).Returns(AppContext.BaseDirectory);
        environment.SetupGet(e => e.ContentRootFileProvider).Returns(new NullFileProvider());
        return environment.Object;
    }

    private static async Task<JsonDocument> ReadJsonResponseAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
