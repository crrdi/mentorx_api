using Microsoft.EntityFrameworkCore;
using MentorX.Domain.Entities;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;

namespace MentorX.Infrastructure.Data.Repositories;

public class TagRepository : Repository<Tag>, ITagRepository
{
    public TagRepository(MentorXDbContext context) : base(context)
    {
    }

    public async Task<Tag?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task<List<Tag>> GetOrCreateManyAsync(IEnumerable<string> names)
    {
        var list = names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
        if (list.Count == 0)
            return new List<Tag>();

        var result = new List<Tag>();
        foreach (var name in list)
        {
            var tag = await GetByNameAsync(name);
            if (tag == null)
            {
                tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _dbSet.AddAsync(tag);
                result.Add(tag);
            }
            else
            {
                result.Add(tag);
            }
        }

        return result;
    }

    public async Task<(List<(string Name, int MentorCount, int PostCount)> Items, int Total)> GetPopularTagStatsAsync(string? search, int limit, int offset)
    {
        var baseQuery = _dbSet.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            var lower = search.ToLower();
            baseQuery = baseQuery.Where(t => t.Name.ToLower().Contains(lower));
        }

        var total = await baseQuery.CountAsync();

        var list = await baseQuery
            .Select(t => new { t.Name, MentorCount = t.MentorTags.Count, PostCount = t.InsightTags.Count })
            .OrderByDescending(x => x.MentorCount + x.PostCount)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        var items = list.Select(x => (x.Name, x.MentorCount, x.PostCount)).ToList();
        return (items, total);
    }

    public async Task<List<string>> SearchTagsAsync(string search, int limit)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return new List<string>();
        }

        var lower = search.ToLower();
        var tags = await _dbSet
            .Where(t => t.Name.ToLower().StartsWith(lower))
            .OrderBy(t => t.Name)
            .Take(limit)
            .Select(t => t.Name)
            .ToListAsync();

        return tags;
    }
}
