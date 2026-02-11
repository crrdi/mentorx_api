using MentorX.Domain.Entities;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;

namespace MentorX.Infrastructure.Data.Repositories;

public class CreditTransactionRepository : Repository<CreditTransaction>, ICreditTransactionRepository
{
    public CreditTransactionRepository(MentorXDbContext context) : base(context)
    {
    }
}
