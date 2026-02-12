using Microsoft.EntityFrameworkCore;
using MentorX.Domain.Entities;
using System.Text.Json;

namespace MentorX.Infrastructure.Data.DbContext;

public class MentorXDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public MentorXDbContext(DbContextOptions<MentorXDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Mentor> Mentors { get; set; }
    public DbSet<MentorRole> MentorRoles { get; set; }
    public DbSet<Insight> Insights { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Actor> Actors { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<UserFollowsMentor> UserFollowsMentor { get; set; }
    public DbSet<UserLikes> UserLikes { get; set; }
    public DbSet<CreditPackage> CreditPackages { get; set; }
    public DbSet<CreditTransaction> CreditTransactions { get; set; }
    public DbSet<MentorAutomation> MentorAutomations { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<RevenueCatWebhookEvent> RevenueCatWebhookEvents { get; set; }
    public DbSet<MentorTag> MentorTags { get; set; }
    public DbSet<InsightTag> InsightTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FocusAreas).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );
            entity.Property(e => e.Credits).HasDefaultValue(10);
            entity.Property(e => e.RevenueCatCustomerId).HasMaxLength(255);
            entity.Property(e => e.SubscriptionStatus).HasMaxLength(50);
            entity.Property(e => e.SubscriptionProductId).HasMaxLength(255);
            entity.HasIndex(e => e.RevenueCatCustomerId);
            entity.HasIndex(e => e.SubscriptionStatus);
        });

        // RevenueCatWebhookEvent - idempotency for webhook processing
        modelBuilder.Entity<RevenueCatWebhookEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasMaxLength(255);
        });

        // MentorRole configuration
        modelBuilder.Entity<MentorRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
        });

        // Mentor configuration
        modelBuilder.Entity<Mentor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PublicBio).IsRequired();
            entity.Property(e => e.ExpertisePrompt).IsRequired();
            entity.Property(e => e.Level).HasDefaultValue(1);
            entity.Property(e => e.FollowerCount).HasDefaultValue(0);
            entity.Property(e => e.InsightCount).HasDefaultValue(0);
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Mentors)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.MentorTags)
                .WithOne(mt => mt.Mentor)
                .HasForeignKey(mt => mt.MentorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Insight configuration
        modelBuilder.Entity<Insight>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Quote).HasMaxLength(280);
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            entity.Property(e => e.CommentCount).HasDefaultValue(0);
            entity.Property(e => e.Type).HasConversion<int>();
            
            entity.HasOne(e => e.Mentor)
                .WithMany(m => m.Insights)
                .HasForeignKey(e => e.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.InsightTags)
                .WithOne(it => it.Insight)
                .HasForeignKey(it => it.InsightId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // MentorTag configuration
        modelBuilder.Entity<MentorTag>(entity =>
        {
            entity.HasKey(e => new { e.MentorId, e.TagId });
            entity.HasOne(e => e.Mentor)
                .WithMany(m => m.MentorTags)
                .HasForeignKey(e => e.MentorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Tag)
                .WithMany(t => t.MentorTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // InsightTag configuration
        modelBuilder.Entity<InsightTag>(entity =>
        {
            entity.HasKey(e => new { e.InsightId, e.TagId });
            entity.HasOne(e => e.Insight)
                .WithMany(i => i.InsightTags)
                .HasForeignKey(e => e.InsightId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Tag)
                .WithMany(t => t.InsightTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Comment configuration
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            
            entity.HasOne(e => e.Insight)
                .WithMany(i => i.Comments)
                .HasForeignKey(e => e.InsightId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.AuthorActor)
                .WithMany(a => a.Comments)
                .HasForeignKey(e => e.AuthorActorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Parent)
                .WithMany(c => c.Replies)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Actor configuration
        modelBuilder.Entity<Actor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<int>();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Mentor)
                .WithOne(m => m.Actor)
                .HasForeignKey<Actor>(e => e.MentorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Conversation configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LastMessage).IsRequired();
            entity.Property(e => e.UserUnreadCount).HasDefaultValue(0);
            
            entity.HasIndex(e => new { e.UserId, e.MentorId }).IsUnique();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Mentor)
                .WithMany(m => m.Conversations)
                .HasForeignKey(e => e.MentorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.SenderActor)
                .WithMany(a => a.Messages)
                .HasForeignKey(e => e.SenderActorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserFollowsMentor configuration
        modelBuilder.Entity<UserFollowsMentor>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.MentorId });
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Mentor)
                .WithMany(m => m.Followers)
                .HasForeignKey(e => e.MentorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserLikes configuration
        modelBuilder.Entity<UserLikes>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.InsightId });
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Insight)
                .WithMany(i => i.Likes)
                .HasForeignKey(e => e.InsightId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CreditPackage configuration
        modelBuilder.Entity<CreditPackage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.RevenueCatProductId).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(50).HasDefaultValue("one_time");
        });

        // CreditTransaction configuration
        modelBuilder.Entity<CreditTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.TransactionId).HasMaxLength(255);
            entity.HasIndex(e => e.TransactionId); // Index for idempotency checks
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MentorAutomation configuration
        modelBuilder.Entity<MentorAutomation>(entity =>
        {
            entity.HasKey(e => e.MentorId);
            entity.Property(e => e.Cadence).HasMaxLength(50);
            entity.Property(e => e.Timezone).HasMaxLength(50);
            
            entity.HasOne(e => e.Mentor)
                .WithOne(m => m.Automation)
                .HasForeignKey<MentorAutomation>(e => e.MentorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Soft delete query filter
        modelBuilder.Entity<User>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Mentor>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Insight>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Comment>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Actor>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Conversation>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Message>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<CreditPackage>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<CreditTransaction>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Tag>().HasQueryFilter(e => e.DeletedAt == null);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
}
