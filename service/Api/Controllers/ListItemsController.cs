using Api.Models.Requests;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Global search across all list items
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    public class ListItemsController : BaseApiController
{
        private readonly IListItemService _listItemService;

        public ListItemsController(IListItemService listItemService)
        {
            _listItemService = listItemService;
        }

        /// <summary>
        /// Searches all list items across all lists
        /// </summary>
        /// <param name="criteria">Search criteria including text filter and pagination</param>
        /// <returns>Paginated list of items from all lists</returns>
        /// <response code="200">Returns the search results</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] ListItemSearchCriteria criteria)
        {
            var userId = GetUserId();
            var results = await _listItemService.SearchAsync(userId, criteria);
            return Ok(results);
        }
    }
}
