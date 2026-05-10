using Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize]
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid GetUserId()
    {
        if (HttpContext.Items.TryGetValue(UserContextMiddleware.UserIdKey, out var userId) && userId is Guid guid)
        {
            return guid;
        }

        throw new UnauthorizedAccessException("User ID not found in request context");
    }
}
