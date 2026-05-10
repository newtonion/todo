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
        /// Searches lists with optional filters
        /// </summary>
        /// <param name="criteria">Search criteria including text filter, archived status, and pagination</param>
        /// <returns>Paginated list of lists</returns>
        /// <response code="200">Returns the search results</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] ListSearchCriteria criteria)
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
        public async Task<IActionResult> Rename(Guid id, [FromBody] UpdateListRequest request)
        {
            var userId = GetUserId();
            await _listService.RenameAsync(userId, id, request.Name);
            return NoContent();
        }

        /// <summary>
        /// Archives a list (marks as closed)
        /// </summary>
        /// <param name="id">The list ID</param>
        /// <response code="204">List archived successfully</response>
        /// <response code="404">If the list is not found</response>
        [HttpPost("{id}/close")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Close(Guid id)
        {
            var userId = GetUserId();
            await _listService.CloseAsync(userId, id);
            return NoContent();
        }

        /// <summary>
        /// Unarchives a list (marks as open)
        /// </summary>
        /// <param name="id">The list ID</param>
        /// <response code="204">List unarchived successfully</response>
        /// <response code="404">If the list is not found</response>
        [HttpPost("{id}/open")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Open(Guid id)
        {
            var userId = GetUserId();
            await _listService.OpenAsync(userId, id);
            return NoContent();
        }

        /// <summary>
        /// Deletes a list and all its items
        /// </summary>
        /// <param name="id">The list ID</param>
        /// <response code="204">List deleted successfully</response>
        /// <response code="404">If the list is not found</response>
        /// <remarks>
        /// Warning: This will cascade delete all items in the list
        /// </remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            await _listService.DeleteAsync(userId, id);
            return NoContent();
        }
    }
}
