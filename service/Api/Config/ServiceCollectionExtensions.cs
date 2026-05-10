using Api.Infrastructure;
using Api.Infrastructure.Entities;
using Api.Services;
using Api.Validators;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;


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
        var useClerk = configuration.GetValue<bool>("Authentication:UseClerk", true);

        if (useClerk)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = configuration["Clerk:Authority"];
                    options.Audience = configuration["Clerk:Audience"];
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true
                    };
                });
        }
        else
        {
            // Development mode - minimal authentication for testing - requires JWT tokens with any content, no validation
            /*services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = false
                    };
                });
            */

        services.AddAuthentication("DevAuth")
            .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevAuth", null);
        }

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
