using Api.Infrastructure;
using Api.Infrastructure.Entities;
using Api.Services;
using Api.Validators;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


namespace Api.Config;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddPooledDbContextFactory<TodoDatabaseContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=data/todo.db"));

        // Application Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IListService, ListService>();
        services.AddScoped<IListItemService, ListItemService>();
        services.AddScoped<ICategoryService, CategoryService>();

        // Validators
        services.AddScoped<IEntityValidator<ListEntity>, ListEntityValidator>();
        services.AddScoped<IEntityValidator<ListItemEntity>, ListItemEntityValidator>();
        services.AddScoped<IEntityValidator<CategoryEntity>, CategoryEntityValidator>();

        // Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Clerk:Authority"];
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var allowedOrigins = configuration
                            .GetSection("Cors:AllowedOrigins")
                            .Get<string[]>() ?? [];

                        var authorizedParty = context.Principal?.FindFirst("azp")?.Value;

                        if (!string.IsNullOrEmpty(authorizedParty) &&
                            allowedOrigins.Length > 0 &&
                            !allowedOrigins.Contains(authorizedParty, StringComparer.OrdinalIgnoreCase))
                        {
                            context.Fail($"Invalid authorized party: {authorizedParty}");
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        // CORS for frontend
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(
                        configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
