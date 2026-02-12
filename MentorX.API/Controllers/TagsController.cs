using Microsoft.AspNetCore.Mvc;
using MentorX.Application.Interfaces;

namespace MentorX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularTags(
        [FromQuery] string? search = null,
        [FromQuery] int limit = 5,
        [FromQuery] int offset = 0)
    {
        try
        {
            var result = await _tagService.GetPopularTagsAsync(search, limit, offset);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Tag autocomplete endpoint - Returns tag names that start with the search query
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchTags(
        [FromQuery] string search,
        [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return Ok(new List<string>());
            }

            var tags = await _tagService.SearchTagsAsync(search, limit);
            return Ok(tags);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
