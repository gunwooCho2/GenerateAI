using Core.Entity;
using Core.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Data;

public class GenerateAiDbContext(DbContextOptions<GenerateAiDbContext> options) : DbContext(options)
{
    public const string DatabaseSchemaName = "GenerateAI";

    public DbSet<ModelEntity> Models => Set<ModelEntity>();
    public DbSet<ContentEntity> Contents => Set<ContentEntity>();
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<OutputCacheEntity> OutputCaches => Set<OutputCacheEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DatabaseSchemaName);

        modelBuilder.Entity<ModelEntity>(entity =>
        {
            entity.HasKey(model => model.Id);
            entity.Property(model => model.ModelName).IsRequired().HasMaxLength(64);
            entity.HasIndex(model => model.ModelName).IsUnique();

            entity.HasData(
                System.Enum.GetValues<Model>()
                    .Select(model => new ModelEntity
                    {
                        Id = (int)model + 1,
                        ModelName = model.GetModelName()
                    }));
        });

        modelBuilder.Entity<ContentEntity>(entity =>
        {
            entity.HasKey(content => content.Id);
            entity.Property(content => content.Role).HasConversion<string>().HasMaxLength(32);
            entity.Property(content => content.Message).IsRequired();
            entity.Property(content => content.Hash).HasMaxLength(128);
            entity.HasIndex(content => content.Hash);
            entity.HasOne(content => content.Model)
                .WithMany()
                .HasForeignKey(content => content.ModelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConversationEntity>(entity =>
        {
            entity.HasKey(conversation => conversation.Id);
            entity.HasOne(conversation => conversation.ParentConversation)
                .WithMany(conversation => conversation.Children)
                .HasForeignKey(conversation => conversation.ParentConversationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(conversation => conversation.InputContent)
                .WithMany()
                .HasForeignKey(conversation => conversation.InputContentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(conversation => conversation.OutputContent)
                .WithMany()
                .HasForeignKey(conversation => conversation.OutputContentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OutputCacheEntity>(entity =>
        {
            entity.HasKey(cache => cache.Id);
            entity.Property(cache => cache.Provider).HasConversion<string>().HasMaxLength(32);
            entity.Property(cache => cache.CacheType).HasConversion<string>().HasMaxLength(64);
            entity.Property(cache => cache.CacheKey).IsRequired().HasMaxLength(256);
            entity.HasIndex(cache => cache.CacheKey).IsUnique();
            entity.HasIndex(cache => new { cache.OutputContentId, cache.Provider });
            entity.HasOne(cache => cache.OutputContent)
                .WithMany()
                .HasForeignKey(cache => cache.OutputContentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(cache => cache.Model)
                .WithMany()
                .HasForeignKey(cache => cache.ModelId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
