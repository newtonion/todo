using Api.Models.Requests;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Manages items within a specific todo list
    /// </summary>
    [Authorize]
    [Route("api/lists/{listId}/items")]
    public class ListItemController : BaseApiController
    {
        private readonly IListItemService _listItemService;

        public ListItemController(IListItemService listItemService)
        {
            _listItemService = listItemService;
        }

        /// <summary>
        /// Creates a new item in a list
        /// </summary>
        /// <param name="listId">The list ID</param>
        /// <param name="request">Item creation details including name and optional due date</param>
        /// <returns>The ID of the created item</returns>
        /// <response code="200">Returns the newly created item ID</response>
        /// <response code="400">If the item data is invalid or list doesn't exist</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(Guid listId, [FromBody] CreateListItemRequest request)
        {
            var userId = GetUserId();
            var newItemId = await _listItemService.CreateAsync(userId, listId, request.Name, request.DueDate);
            return Ok(new { Id = newItemId });
        }

        /// <summary>
        /// Searches items within a specific list
        /// </summary>
        /// <param name="listId">The list ID</param>
        /// <param name="criteria">Search criteria including text filter and pagination</param>
        /// <returns>Paginated list of items</returns>
        /// <response code="200">Returns the search results</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(Guid listId, [FromQuery] ListItemSearchCriteria criteria)
        {
            var userId = GetUserId();
            // Override the ListId from route
            criteria.ListId = listId;
            var results = await _listItemService.SearchAsync(userId, criteria);
            return Ok(results);
        }

        /// <summary>
        /// Gets an item by ID
        /// </summary>
        /// <param name="itemId">The item ID</param>
        /// <returns>The item details</returns>
        /// <response code="200">Returns the item</response>
        /// <response code="404">If the item is not found</response>
        [HttpGet("{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(Guid itemId)
        {
            var userId = GetUserId();
            var result = await _listItemService.GetAsync(userId, itemId);
            return Ok(result);
        }

        /// <summary>
        /// Renames an item
        /// </summary>
        /// <param name="itemId">The item ID</param>
        /// <param name="request">The new name</param>
        /// <response code="204">Item renamed successfully</response>
        /// <response code="404">If the item is not found</response>
        /// <response code="400">If the name is invalid</response>
        [HttpPost("{itemId}/rename")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Rename(Guid itemId, [FromBody] RenameListItemRequest request)
        {
            var userId = GetUserId();
            await _listItemService.RenameAsync(userId, itemId, request.Name);
            return NoContent();
        }

        /// <summary>
        /// Sets or updates the due date for an item
        /// </summary>
        /// <param name="itemId">The item ID</param>
        /// <param name="request">The due date (null to clear)</param>
        /// <response code="204">Due date updated successfully</response>
        /// <response code="404">If the item is not found</response>
        [HttpPost("{itemId}/due-date")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetDueDate(Guid itemId, [FromBody] SetListItemDueDateRequest request)
        {
            var userId = GetUserId();
            await _listItemService.SetDueDateAsync(userId, itemId, request.DueDate);
            return NoContent();
        }

        /// <summary>
        /// Updates the sort index for an item (for reordering)
        /// </summary>
        /// <param name="itemId">The item ID</param>
        /// <param name="request">The new sort index</param>
        /// <response code="204">Sort index updated successfully</response>
        /// <response code="404">If the item is not found</response>
        [HttpPost("{itemId}/reorder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reorder(Guid itemId, [FromBody] SetListItemSortIndexRequest request)
        {
            var userId = GetUserId();
            await _listItemService.SetSortIndexAsync(userId, itemId, request.SortIndex);
            return NoContent();
        }

        /// <summary>
        /// Deletes an item
        /// </summary>
        /// <param name="itemId">The item ID</param>
        /// <response code="204">Item deleted successfully</response>
        /// <response code="404">If the item is not found</response>
        [HttpDelete("{itemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid itemId)
        {
            var userId = GetUserId();
            await _listItemService.DeleteAsync(userId, itemId);
            return NoContent();
        }

        /// <summary>
        /// Toggles an item's completion status
        /// </summary>
        /// <param name="itemId">The item ID</param>
        /// <response code="204">Item completion status toggled successfully</response>
        /// <response code="404">If the item is not found</response>
        [HttpPost("{itemId}/toggle")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleCompletion(Guid itemId)
        {
            var userId = GetUserId();
            await _listItemService.ToggleCompletionAsync(userId, itemId);
            return NoContent();
        }
    }
}
