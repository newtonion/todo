using System.Security.Claims;
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

    public async Task InvokeAsync(HttpContext context, IUserService userService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Clerk authentication - get sub claim and lookup/create user
            var clerkUserId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(clerkUserId))
            {
                _logger.LogWarning("Authenticated user missing 'sub' claim");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            // Lazy create/lookup user
            var userId = await userService.GetOrCreateUserAsync(clerkUserId);
            
            // Store user ID in HttpContext for controllers to access
            context.Items[UserIdKey] = userId;
        }

        await _next(context);
    }
}
