using MentorX.Domain.Entities;

namespace MentorX.Domain.Interfaces;

public interface ITagRepository
{
    Task<Tag?> GetByNameAsync(string name);
    Task<List<Tag>> GetOrCreateManyAsync(IEnumerable<string> names);
    Task<(List<(string Name, int MentorCount, int PostCount)> Items, int Total)> GetPopularTagStatsAsync(string? search, int limit, int offset);
}
