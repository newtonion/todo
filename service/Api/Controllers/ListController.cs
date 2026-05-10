using Api.Models.Requests;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Manages todo lists
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    public class ListController : BaseApiController
    {
        private readonly IListService _listService;

        public ListController(IListService listService)
        {
            _listService = listService;
        }

        /// <summary>
        /// Creates a new todo list
        /// </summary>
        /// <param name="request">List creation details including name and category</param>
        /// <returns>The ID of the created list</returns>
        /// <response code="200">Returns the newly created list ID</response>
        /// <response code="400">If the list data is invalid or category doesn't exist</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateListRequest request)
        {
            var userId = GetUserId();
            var newListId = await _listService.CreateAsync(userId, request.Name, request.CategoryId);
            return Ok(new { Id = newListId });
        }

        /// <summary>
        /// Gets a list by ID with item counts
        /// </summary>
        /// <param name="id">The list ID</param>
        /// <returns>The list details including items</returns>
        /// <response code="200">Returns the list</response>
        /// <response code="404">If the list is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(Guid id)
        {
            var userId = GetUserId();
            var result = await _listService.GetAsync(userId, id);
            return Ok(result);
        }

        /// <summary>
        /// Gets counts for a list including total items and completed items
        /// </summary>
        /// <param name="id">The list ID</param>
        /// <returns>Item counts for the list</returns>
        /// <response code="200">Returns the list counts</response>
        /// <response code="404">If the list is not found</response>
        [HttpGet("{id}/counts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCounts(Guid id)
        {
            var userId = GetUserId();
            var result = await _listService.GetCountsAsync(userId, id);
            return Ok(result);
        }

        /// <summary>
        /// Searches lists with optional filters
        /// </summary>
        /// <param name="criteria">Search criteria including text filter, archived status, and pagination</param>
        /// <returns>Paginated list of lists</returns>
        /// <response code="200">Returns the search results</response>
        [HttpPost("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromBody] ListSearchCriteria criteria)
        {
            var userId = GetUserId();
            var results = await _listService.SearchAsync(userId, criteria);
            return Ok(results);
        }

        /// <summary>
        /// Renames a list
        /// </summary>
        /// <param name="id">The list ID</param>
        /// <param name="request">Updated list data</param>
        /// <response code="204">List updated successfully</response>
        /// <response code="404">If the list is not found</response>
        /// <response code="400">If the list data is invalid</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Rename(Guid id, [FromBody] RenameListRequest request)
        {
            var userId = GetUserId();
            await _listService.RenameAsync(userId, id, request.Name);
            return NoContent();
        }

        /// <summary>
        /// Toggles the completion status of a list
        /// </summary>
        /// <param name="id">The list ID</param>
        /// <response code="204">List completion status toggled successfully</response>
        /// <response code="404">If the list is not found</response>
        [HttpPost("{id}/complete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleComplete(Guid id)
        {
            var userId = GetUserId();
            await _listService.ToggleCompleteAsync(userId, id);
            return NoContent();
        }

        /// <summary>
        /// Archives a list and all its items
        /// </summary>
        /// <param name="id">The list ID</param>
        /// <response code="204">List archived successfully</response>
        /// <response code="404">If the list is not found</response>
        [HttpPost("{id}/archive")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleArchive(Guid id)
        {
            var userId = GetUserId();
            await _listService.ToggleArchiveAsync(userId, id);
            return NoContent();
        }

        /// <summary>
        /// Updates the category of a list
        /// </summary>
        /// <param name="id">The list ID</param>
        /// <param name="request">Updated list category data</param>
        /// <response code="204">List category updated successfully</response>
        /// <response code="404">If the list or category (if provided) is not found</response>

        [HttpPost("{id}/category")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetCategory(Guid id, [FromBody] UpdateListCategoryRequest request)
        {
            var userId = GetUserId();
            await _listService.SetCategoryAsync(userId, id, request.Category);
            return NoContent();
        }
    }
}
