using MentorX.Domain.Entities;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;
using Microsoft.EntityFrameworkCore;

namespace MentorX.Infrastructure.Data.Repositories;

public class CreditPackageRepository : Repository<CreditPackage>, ICreditPackageRepository
{
    public CreditPackageRepository(MentorXDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<CreditPackage>> GetAllAsync()
    {
        return await _dbSet.OrderBy(p => p.Credits).ToListAsync();
    }
}
