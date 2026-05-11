using Api.Services;
using Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using static Api.Tests.TestSupport.TestEntities;
using Xunit;

namespace Api.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task GetOrCreateUserAsync_WhenUserExists_ReturnsExistingUserId()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var existingUser = AddUser(context, "auth-123", "Existing User");
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var userId = await service.GetOrCreateUserAsync("auth-123");

        Assert.Equal(existingUser.Id, userId);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WhenUserExists_DoesNotCreateDuplicateOrUpdateName()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var existingUser = AddUser(context, "auth-123", "Existing User");
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await service.GetOrCreateUserAsync("auth-123");

        context.ChangeTracker.Clear();
        var users = await context.Users.ToListAsync();
        var user = Assert.Single(users);
        Assert.Equal(existingUser.Id, user.Id);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WhenUserDoesNotExist_CreatesAndReturnsUserId()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var service = CreateService(database);

        var userId = await service.GetOrCreateUserAsync("auth-456");

        var user = await context.Users.SingleAsync(u => u.Id == userId);
        Assert.Equal("auth-456", user.AuthId);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WhenDifferentAuthIdExists_CreatesSeparateUser()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var existingUser = AddUser(context, "auth-123", "Existing User");
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var newUserId = await service.GetOrCreateUserAsync("auth-456");

        Assert.NotEqual(existingUser.Id, newUserId);
        Assert.Equal(2, await context.Users.CountAsync());
        Assert.NotNull(await context.Users.SingleOrDefaultAsync(u => u.AuthId == "auth-456"));
    }

    private static UserService CreateService(TestDatabase database)
    {
        return new UserService(
            new TestDbContextFactory(database.Options),
            NullLogger<UserService>.Instance);
    }
}
