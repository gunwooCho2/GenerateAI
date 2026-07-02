using Core.Data;
using Core.Entity;
using Core.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Test;

public static class TestDbGenerateCache
{
    public static async Task<string> InsertSampleConversationAsync()
    {
        await using GenerateAiDbContext? dbContext = GenerateAiDbContextFactory.CreateFromEnvironment();
        if (dbContext == null)
        {
            return $"DB disabled: {GenerateAiDbContextFactory.ConnectionStringEnvironmentName} is not set.";
        }

        DateTime now = DateTime.UtcNow;
        int modelId = (int)Model.Gpt + 1;

        ContentEntity input = new()
        {
            Role = ContentRole.Input,
            Message = "Hello from TestDbGenerateCache.",
            Hash = Guid.NewGuid().ToString("N"),
            ModelId = modelId,
            CreatedAt = now
        };

        ContentEntity output = new()
        {
            Role = ContentRole.Output,
            Message = "Hello. This is a cached output sample.",
            Hash = null,
            ModelId = modelId,
            CreatedAt = now
        };

        dbContext.Contents.AddRange(input, output);
        await dbContext.SaveChangesAsync();

        ConversationEntity conversation = new()
        {
            ParentConversationId = null,
            InputContentId = input.Id,
            OutputContentId = output.Id,
            CreatedAt = now
        };

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync();

        OutputCacheEntity cache = new()
        {
            OutputContentId = output.Id,
            ModelId = modelId,
            Provider = CacheProvider.Gpt,
            CacheKey = Guid.NewGuid().ToString("N"),
            CacheType = CacheType.ProviderResponse,
            SequenceIndex = null,
            CreatedAt = now,
            ExpiredAt = null
        };

        dbContext.OutputCaches.Add(cache);
        await dbContext.SaveChangesAsync();

        return $"Inserted conversation={conversation.Id}, input={input.Id}, output={output.Id}, cache={cache.Id}.";
    }

    public static async Task<string> CountRowsAsync()
    {
        await using GenerateAiDbContext? dbContext = GenerateAiDbContextFactory.CreateFromEnvironment();
        if (dbContext == null)
        {
            return $"DB disabled: {GenerateAiDbContextFactory.ConnectionStringEnvironmentName} is not set.";
        }

        int contents = await dbContext.Contents.CountAsync();
        int conversations = await dbContext.Conversations.CountAsync();
        int caches = await dbContext.OutputCaches.CountAsync();

        return $"Contents={contents}, Conversations={conversations}, OutputCaches={caches}.";
    }
}
