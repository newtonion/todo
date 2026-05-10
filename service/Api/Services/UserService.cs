using System;
using Api.Infrastructure;
using Api.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public interface IUserService
{
    Task<Guid> GetOrCreateUserAsync(string authId, string name, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
    private readonly IDbContextFactory<TodoDatabaseContext> _dbContextFactory;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IDbContextFactory<TodoDatabaseContext> dbContextFactory,
        ILogger<UserService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<Guid> GetOrCreateUserAsync(string authId, string name, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Try to find existing user by AuthId
        var existingUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.AuthId == authId, cancellationToken);

        if (existingUser != null)
        {
            return existingUser.Id;
        }

        // User doesn't exist, create new one
        var newUser = new UserEntity
        {
            AuthId = authId,
            Name = name
        };

        dbContext.Users.Add(newUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new user {UserId} for auth ID {AuthId}", newUser.Id, authId);

        return newUser.Id;
    }
}
