using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.TestSupport;

public sealed class TestDatabase
{
    public TestDatabase()
    {
        Options = new DbContextOptionsBuilder<TodoDatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public DbContextOptions<TodoDatabaseContext> Options { get; }

    public TodoDatabaseContext CreateContext() => new(Options);
}

public sealed class TestDbContextFactory(DbContextOptions<TodoDatabaseContext> options) : IDbContextFactory<TodoDatabaseContext>
{
    public TodoDatabaseContext CreateDbContext() => new(options);

    public Task<TodoDatabaseContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }
}
