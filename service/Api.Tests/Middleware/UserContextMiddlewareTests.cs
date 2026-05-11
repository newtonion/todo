using System.Security.Claims;
using Api.Middleware;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Api.Tests.Middleware;

public class UserContextMiddlewareTests
{
    private static readonly Guid ClerkUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task InvokeAsync_WhenAuthenticatedClerkUserHasSubClaim_StoresResolvedUserId()
    {
        var context = new DefaultHttpContext
        {
            User = CreatePrincipal(("sub", "clerk-user-123"))
        };
        var nextWasCalled = false;
        var middleware = new UserContextMiddleware(
            ctx =>
            {
                nextWasCalled = true;
                Assert.Equal(ClerkUserId, ctx.Items[UserContextMiddleware.UserIdKey]);
                return Task.CompletedTask;
            },
            NullLogger<UserContextMiddleware>.Instance);
        var userService = new Mock<IUserService>();
        userService
            .Setup(s => s.GetOrCreateUserAsync("clerk-user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClerkUserId);

        await middleware.InvokeAsync(context, userService.Object);

        Assert.True(nextWasCalled);
    }


    [Fact]
    public async Task InvokeAsync_WhenAuthenticatedClerkUserIsMissingSubClaim_ReturnsUnauthorizedAndDoesNotCallNext()
    {
        var context = new DefaultHttpContext
        {
            User = CreatePrincipal(("name", "Clerk User"))
        };
        var nextWasCalled = false;
        var middleware = new UserContextMiddleware(
            _ =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<UserContextMiddleware>.Instance);
        var userService = new Mock<IUserService>();

        await middleware.InvokeAsync(context, userService.Object);

        Assert.False(nextWasCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(context.Items.ContainsKey(UserContextMiddleware.UserIdKey));
        userService.Verify(s => s.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenClerkUserIsNotAuthenticated_CallsNextWithoutUserId()
    {
        var context = new DefaultHttpContext();
        var nextWasCalled = false;
        var middleware = new UserContextMiddleware(
            ctx =>
            {
                nextWasCalled = true;
                Assert.False(ctx.Items.ContainsKey(UserContextMiddleware.UserIdKey));
                return Task.CompletedTask;
            },
            NullLogger<UserContextMiddleware>.Instance);

        await middleware.InvokeAsync(context, Mock.Of<IUserService>());

        Assert.True(nextWasCalled);
    }

    private static ClaimsPrincipal CreatePrincipal(params (string Type, string Value)[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            claims.Select(c => new Claim(c.Type, c.Value)),
            authenticationType: "Test"));
    }
}
