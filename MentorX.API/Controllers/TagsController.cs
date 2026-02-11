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
}
