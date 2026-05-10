using Api.Models.Requests;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Manages todo list categories
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    public class UserController : BaseApiController
    {
        public UserController()
        {
        }

        /// <summary>
        /// Dummy endpoint we can call to create a user record for the authenticated user if it doesn't exist yet.
        /// We create it from the middleware, and call this right after the login flow completes.
        /// </summary>
        [HttpGet()]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Negotiate()
        {
            return Ok();
        }
    }
}
