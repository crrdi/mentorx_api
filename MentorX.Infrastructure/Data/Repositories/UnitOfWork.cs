using Microsoft.EntityFrameworkCore.Storage;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;
using Microsoft.Extensions.Logging;

namespace MentorX.Infrastructure.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MentorXDbContext _context;
    private readonly ILoggerFactory _loggerFactory;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _users;
    private IMentorRepository? _mentors;
    private IInsightRepository? _insights;
    private ICommentRepository? _comments;
    private IConversationRepository? _conversations;
    private IMessageRepository? _messages;
    private IActorRepository? _actors;
    private IUserFollowsMentorRepository? _userFollowsMentor;
    private IUserLikesRepository? _userLikes;
    private ITagRepository? _tags;
    private ICreditPackageRepository? _creditPackages;
    private ICreditTransactionRepository? _creditTransactions;

    public UnitOfWork(MentorXDbContext context, ILoggerFactory loggerFactory)
    {
        _context = context;
        _loggerFactory = loggerFactory;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IMentorRepository Mentors => _mentors ??= new MentorRepository(_context);
    public IInsightRepository Insights => _insights ??= new InsightRepository(_context, _loggerFactory.CreateLogger<InsightRepository>());
    public ICommentRepository Comments => _comments ??= new CommentRepository(_context);
    public IConversationRepository Conversations => _conversations ??= new ConversationRepository(_context);
    public IMessageRepository Messages => _messages ??= new MessageRepository(_context);
    public IActorRepository Actors => _actors ??= new ActorRepository(_context);
    public IUserFollowsMentorRepository UserFollowsMentor => _userFollowsMentor ??= new UserFollowsMentorRepository(_context);
    public IUserLikesRepository UserLikes => _userLikes ??= new UserLikesRepository(_context);
    public ITagRepository Tags => _tags ??= new TagRepository(_context);
    public ICreditPackageRepository CreditPackages => _creditPackages ??= new CreditPackageRepository(_context);
    public ICreditTransactionRepository CreditTransactions => _creditTransactions ??= new CreditTransactionRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
