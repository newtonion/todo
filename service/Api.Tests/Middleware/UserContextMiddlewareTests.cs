using System.Security.Claims;
using Api.Middleware;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Api.Tests.Middleware;

public class UserContextMiddlewareTests
{
    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ClerkUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task InvokeAsync_WhenClerkIsDisabled_UsesConfiguredTestUserId()
    {
        var context = new DefaultHttpContext();
        var nextWasCalled = false;
        var middleware = new UserContextMiddleware(
            ctx =>
            {
                nextWasCalled = true;
                Assert.Equal(TestUserId, ctx.Items[UserContextMiddleware.UserIdKey]);
                return Task.CompletedTask;
            },
            NullLogger<UserContextMiddleware>.Instance);
        var userService = new Mock<IUserService>();

        await middleware.InvokeAsync(context, userService.Object, CreateConfiguration(("Authentication:UseClerk", "false"), ("Authentication:TestUserId", TestUserId.ToString())));

        Assert.True(nextWasCalled);
        userService.Verify(s => s.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenClerkIsDisabledAndTestUserIdMissing_UsesDefaultTestUserId()
    {
        var context = new DefaultHttpContext();
        var middleware = new UserContextMiddleware(_ => Task.CompletedTask, NullLogger<UserContextMiddleware>.Instance);

        await middleware.InvokeAsync(context, Mock.Of<IUserService>(), CreateConfiguration(("Authentication:UseClerk", "false")));

        Assert.Equal(TestUserId, context.Items[UserContextMiddleware.UserIdKey]);
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthenticatedClerkUserHasSubClaim_StoresResolvedUserId()
    {
        var context = new DefaultHttpContext
        {
            User = CreatePrincipal(("sub", "clerk-user-123"), ("name", "Clerk User"))
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
            .Setup(s => s.GetOrCreateUserAsync("clerk-user-123", "Clerk User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClerkUserId);

        await middleware.InvokeAsync(context, userService.Object, CreateConfiguration(("Authentication:UseClerk", "true")));

        Assert.True(nextWasCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenJwtHandlerMapsSubClaimToNameIdentifier_StoresResolvedUserId()
    {
        var context = new DefaultHttpContext
        {
            User = CreatePrincipal((ClaimTypes.NameIdentifier, "clerk-user-123"), (ClaimTypes.Name, "Clerk User"))
        };
        var middleware = new UserContextMiddleware(_ => Task.CompletedTask, NullLogger<UserContextMiddleware>.Instance);
        var userService = new Mock<IUserService>();
        userService
            .Setup(s => s.GetOrCreateUserAsync("clerk-user-123", "Clerk User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClerkUserId);

        await middleware.InvokeAsync(context, userService.Object, CreateConfiguration(("Authentication:UseClerk", "true")));

        Assert.Equal(ClerkUserId, context.Items[UserContextMiddleware.UserIdKey]);
    }

    [Fact]
    public async Task InvokeAsync_WhenNameClaimIsMissing_UsesPreferredUsername()
    {
        var context = new DefaultHttpContext
        {
            User = CreatePrincipal(("sub", "clerk-user-123"), ("preferred_username", "preferred-user"))
        };
        var middleware = new UserContextMiddleware(_ => Task.CompletedTask, NullLogger<UserContextMiddleware>.Instance);
        var userService = new Mock<IUserService>();
        userService
            .Setup(s => s.GetOrCreateUserAsync("clerk-user-123", "preferred-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClerkUserId);

        await middleware.InvokeAsync(context, userService.Object, CreateConfiguration(("Authentication:UseClerk", "true")));

        userService.VerifyAll();
    }

    [Fact]
    public async Task InvokeAsync_WhenNameClaimsAreMissing_UsesUnknownUser()
    {
        var context = new DefaultHttpContext
        {
            User = CreatePrincipal(("sub", "clerk-user-123"))
        };
        var middleware = new UserContextMiddleware(_ => Task.CompletedTask, NullLogger<UserContextMiddleware>.Instance);
        var userService = new Mock<IUserService>();
        userService
            .Setup(s => s.GetOrCreateUserAsync("clerk-user-123", "Unknown User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClerkUserId);

        await middleware.InvokeAsync(context, userService.Object, CreateConfiguration(("Authentication:UseClerk", "true")));

        userService.VerifyAll();
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

        await middleware.InvokeAsync(context, userService.Object, CreateConfiguration(("Authentication:UseClerk", "true")));

        Assert.False(nextWasCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(context.Items.ContainsKey(UserContextMiddleware.UserIdKey));
        userService.Verify(s => s.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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

        await middleware.InvokeAsync(context, Mock.Of<IUserService>(), CreateConfiguration(("Authentication:UseClerk", "true")));

        Assert.True(nextWasCalled);
    }

    private static IConfiguration CreateConfiguration(params (string Key, string? Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
    }

    private static ClaimsPrincipal CreatePrincipal(params (string Type, string Value)[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            claims.Select(c => new Claim(c.Type, c.Value)),
            authenticationType: "Test"));
    }
}
