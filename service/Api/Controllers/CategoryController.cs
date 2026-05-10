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
        /// Searches categories with optional filters
        /// </summary>
        /// <param name="criteria">Search criteria including text filter and pagination</param>
        /// <returns>Paginated list of categories</returns>
        /// <response code="200">Returns the search results</response>
        [HttpPost("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromBody] CategorySearchCriteria criteria)
        {
            var userId = GetUserId();
            var results = await _categoryService.SearchAsync(userId, criteria);
            return Ok(results);
        }

    }
}
