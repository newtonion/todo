using Api.Infrastructure;
using Api.Infrastructure.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Tests.Integration;

public sealed class TodoApiFactory : WebApplicationFactory<Program>
{
    public static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:TestUserId"] = TestUserId.ToString(),
                ["Cors:AllowedOrigins:0"] = "https://localhost",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Api.Middleware.ExceptionHandlingMiddleware"] = "None",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore.Query"] = "Error"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextFactory<TodoDatabaseContext>>();
            services.RemoveAll<DbContextOptions<TodoDatabaseContext>>();
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestAuth";
                    options.DefaultChallengeScheme = "TestAuth";
                    options.DefaultScheme = "TestAuth";
                })
                .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("TestAuth", null);

            _connection.Open();

            services.AddDbContextFactory<TodoDatabaseContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    public HttpClient CreateApiClient()
    {
        EnsureDatabaseCreated();

        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    public async Task SeedAsync(Func<TodoDatabaseContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TodoDatabaseContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
        await EnsureTestUserAsync(context);
        await seed(context);
        await context.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }

    private void EnsureDatabaseCreated()
    {
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TodoDatabaseContext>>();
        using var context = factory.CreateDbContext();
        context.Database.EnsureCreated();

        if (!context.Users.Any(u => u.Id == TestUserId))
        {
            context.Users.Add(new UserEntity
            {
                Id = TestUserId,
                AuthId = "dev-user",
                Name = "Development User"
            });
            context.SaveChanges();
        }
    }

    private static async Task EnsureTestUserAsync(TodoDatabaseContext context)
    {
        if (!await context.Users.AnyAsync(u => u.Id == TestUserId))
        {
            context.Users.Add(new UserEntity
            {
                Id = TestUserId,
                AuthId = "dev-user",
                Name = "Development User"
            });
            await context.SaveChangesAsync();
        }
    }
}
