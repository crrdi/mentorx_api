using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface ITagService
{
    Task<PagedResponse<TagResponse>> GetPopularTagsAsync(string? search, int limit, int offset);
}
