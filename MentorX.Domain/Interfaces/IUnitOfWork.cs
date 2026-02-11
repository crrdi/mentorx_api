namespace MentorX.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IMentorRepository Mentors { get; }
    IInsightRepository Insights { get; }
    ICommentRepository Comments { get; }
    IConversationRepository Conversations { get; }
    IMessageRepository Messages { get; }
    IActorRepository Actors { get; }
    IUserFollowsMentorRepository UserFollowsMentor { get; }
    IUserLikesRepository UserLikes { get; }
    ITagRepository Tags { get; }
    ICreditPackageRepository CreditPackages { get; }
    ICreditTransactionRepository CreditTransactions { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
