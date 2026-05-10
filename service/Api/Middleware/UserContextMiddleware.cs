using Api.Services;

namespace Api.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    public const string UserIdKey = "UserId";

    public UserContextMiddleware(
        RequestDelegate next,
        ILogger<UserContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserService userService, IConfiguration configuration)
    {
        var useClerk = configuration.GetValue<bool>("Authentication:UseClerk", true);

        Guid userId;

        if (!useClerk)
        {
            // Development mode - use test user without requiring authentication
            var testUserId = configuration.GetValue<string>("Authentication:TestUserId");
            userId = Guid.Parse(testUserId ?? "00000000-0000-0000-0000-000000000001");
            context.Items[UserIdKey] = userId;
            _logger.LogInformation("Development mode: Using test user ID {UserId}", userId);
        }
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            // Clerk authentication - get sub claim and lookup/create user
            var clerkUserId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            
            if (string.IsNullOrEmpty(clerkUserId))
            {
                _logger.LogWarning("Authenticated user missing 'sub' claim");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var userName = context.User.Claims.FirstOrDefault(c => c.Type == "name")?.Value 
                ?? context.User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                ?? "Unknown User";

            // Lazy create/lookup user
            userId = await userService.GetOrCreateUserAsync(clerkUserId, userName);
            
            // Store user ID in HttpContext for controllers to access
            context.Items[UserIdKey] = userId;
        }

        await _next(context);
    }
}
