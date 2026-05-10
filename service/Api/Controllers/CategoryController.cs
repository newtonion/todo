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
    public class CategoryController : BaseApiController
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Creates a new category
        /// </summary>
        /// <param name="request">Category creation details</param>
        /// <returns>The ID of the created category</returns>
        /// <response code="200">Returns the newly created category ID</response>
        /// <response code="400">If the category data is invalid</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            var userId = GetUserId();
            var newCategoryId = await _categoryService.CreateAsync(userId, request.Name);
            return Ok(new { Id = newCategoryId });
        }

        /// <summary>
        /// Gets a category by ID
        /// </summary>
        /// <param name="id">The category ID</param>
        /// <returns>The category details</returns>
        /// <response code="200">Returns the category</response>
        /// <response code="404">If the category is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(Guid id)
        {
            var userId = GetUserId();
            var result = await _categoryService.GetAsync(userId, id);
            return Ok(result);
        }

        /// <summary>
        /// Searches categories with optional filters
        /// </summary>
        /// <param name="criteria">Search criteria including text filter and pagination</param>
        /// <returns>Paginated list of categories</returns>
        /// <response code="200">Returns the search results</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] CategorySearchCriteria criteria)
        {
            var userId = GetUserId();
            var results = await _categoryService.SearchAsync(userId, criteria);
            return Ok(results);
        }

        /// <summary>
        /// Updates a category's name
        /// </summary>
        /// <param name="id">The category ID</param>
        /// <param name="request">Updated category data</param>
        /// <response code="204">Category updated successfully</response>
        /// <response code="404">If the category is not found</response>
        /// <response code="400">If the category data is invalid</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
        {
            var userId = GetUserId();
            await _categoryService.UpdateAsync(userId, id, request.Name);
            return NoContent();
        }

        /// <summary>
        /// Deletes a category
        /// </summary>
        /// <param name="id">The category ID</param>
        /// <response code="204">Category deleted successfully</response>
        /// <response code="404">If the category is not found</response>
        /// <remarks>
        /// Note: Categories that are in use by lists cannot be deleted due to database constraints
        /// </remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            await _categoryService.DeleteAsync(userId, id);
            return NoContent();
        }
    }
}
