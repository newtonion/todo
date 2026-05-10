using Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Tests.Middleware;

public class CorrelationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenRequestHasCorrelationId_UsesExistingCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationMiddleware.HeaderName] = "correlation-123";
        var nextWasCalled = false;
        var middleware = new CorrelationMiddleware(
            ctx =>
            {
                nextWasCalled = true;
                Assert.Equal("correlation-123", ctx.Items[CorrelationMiddleware.HeaderName]);
                return Task.CompletedTask;
            },
            NullLogger<CorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextWasCalled);
        Assert.Equal("correlation-123", context.Response.Headers[CorrelationMiddleware.HeaderName]);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestHasBlankCorrelationId_GeneratesCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationMiddleware.HeaderName] = " ";
        var middleware = new CorrelationMiddleware(_ => Task.CompletedTask, NullLogger<CorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var correlationId = Assert.IsType<string>(context.Items[CorrelationMiddleware.HeaderName]);
        Assert.True(Guid.TryParse(correlationId, out _));
        Assert.Equal(correlationId, context.Response.Headers[CorrelationMiddleware.HeaderName]);
    }
}
