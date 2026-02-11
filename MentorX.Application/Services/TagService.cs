using MentorX.Application.DTOs.Responses;
using MentorX.Application.Interfaces;
using MentorX.Domain.Interfaces;

namespace MentorX.Application.Services;

public class TagService : ITagService
{
    private readonly IUnitOfWork _unitOfWork;

    public TagService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<TagResponse>> GetPopularTagsAsync(string? search, int limit, int offset)
    {
        var (items, total) = await _unitOfWork.Tags.GetPopularTagStatsAsync(search, limit, offset);

        var tagList = items.Select(x => new TagResponse
        {
            Tag = x.Name,
            MentorCount = x.MentorCount,
            PostCount = x.PostCount
        }).ToList();

        return new PagedResponse<TagResponse>
        {
            Items = tagList,
            Total = total,
            HasMore = offset + tagList.Count < total,
            Limit = limit,
            Offset = offset
        };
    }
}
