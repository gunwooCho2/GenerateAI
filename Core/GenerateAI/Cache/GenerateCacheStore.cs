using System.Security.Cryptography;
using System.Text;
using Core.Data;
using Core.Dto;
using Core.Entity;
using Core.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.GenerateAI.Cache;

internal sealed class GenerateCacheStore(GenerateAiDbContext dbContext, ProviderCacheStrategy strategy)
{
    private readonly int _modelId = (int)strategy.Model + 1;

    public async Task<GenerateRequestContext> PrepareAsync(
        List<GenerateInput> inputs,
        int limit,
        CancellationToken cancellationToken = default)
    {
        List<GenerateInput> orderedInputs = inputs.OrderBy(input => input.Turn).ToList();
        if (orderedInputs.Count == 0)
        {
            return new GenerateRequestContext { Inputs = orderedInputs };
        }

        GenerateInput firstInput = orderedInputs[0];

        if (firstInput.CachedConversationId != null)
        {
            ConversationEntity? cachedConversation = await dbContext.Conversations
                .Include(conversation => conversation.InputContent)
                .Include(conversation => conversation.OutputContent)
                .FirstOrDefaultAsync(
                    conversation => conversation.Id == firstInput.CachedConversationId.Value,
                    cancellationToken);

            return await BuildContextFromConversationAsync(orderedInputs, cachedConversation, limit, cancellationToken);
        }

        string hash = HashInput(firstInput.Content);
        ContentEntity? existingInput = await dbContext.Contents
            .Where(content => content.Role == ContentRole.Input
                              && content.ModelId == _modelId
                              && content.Hash == hash)
            .OrderByDescending(content => content.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingInput == null)
        {
            return new GenerateRequestContext { Inputs = orderedInputs };
        }

        ConversationEntity? existingConversation = await dbContext.Conversations
            .Include(conversation => conversation.InputContent)
            .Include(conversation => conversation.OutputContent)
            .Where(conversation => conversation.InputContentId == existingInput.Id)
            .OrderByDescending(conversation => conversation.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return await BuildContextFromConversationAsync(orderedInputs, existingConversation, limit, cancellationToken);
    }

    public async Task<CacheInfo?> SaveAsync(
        List<GenerateInput> inputs,
        string output,
        string providerCacheKey,
        int? sequenceIndex,
        long? parentConversationId = null,
        CancellationToken cancellationToken = default)
    {
        GenerateInput? lastInput = inputs.OrderBy(input => input.Turn).LastOrDefault();
        if (lastInput == null)
        {
            return null;
        }

        DateTime now = DateTime.UtcNow;
        ContentEntity inputContent = new()
        {
            Role = ContentRole.Input,
            Message = lastInput.Content,
            Hash = HashInput(lastInput.Content),
            ModelId = _modelId,
            CreatedAt = now
        };

        ContentEntity outputContent = new()
        {
            Role = ContentRole.Output,
            Message = output,
            Hash = null,
            ModelId = _modelId,
            CreatedAt = now
        };

        dbContext.Contents.AddRange(inputContent, outputContent);
        await dbContext.SaveChangesAsync(cancellationToken);

        ConversationEntity conversation = new()
        {
            ParentConversationId = parentConversationId,
            InputContentId = inputContent.Id,
            OutputContentId = outputContent.Id,
            CreatedAt = now
        };

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        OutputCacheEntity cache = new()
        {
            OutputContentId = outputContent.Id,
            ModelId = _modelId,
            Provider = strategy.Provider,
            CacheKey = string.IsNullOrWhiteSpace(providerCacheKey) ? Guid.NewGuid().ToString("N") : providerCacheKey,
            CacheType = strategy.CacheType,
            SequenceIndex = sequenceIndex,
            CreatedAt = now,
            ExpiredAt = strategy.Provider == CacheProvider.Claude ? now.AddMinutes(5) : null
        };

        dbContext.OutputCaches.Add(cache);
        await dbContext.SaveChangesAsync(cancellationToken);

        CacheInfo cacheInfo = new(cache.CreatedAt, cache.ExpiredAt, cache.CacheKey, cache.SequenceIndex);
        lastInput.SetCacheInfo(strategy.Model, cacheInfo);
        lastInput.SetCachedConversation(conversation.Id);
        lastInput.SetCacheBreakpoint(sequenceIndex);

        return cacheInfo;
    }

    private async Task<GenerateRequestContext> BuildContextFromConversationAsync(
        List<GenerateInput> orderedInputs,
        ConversationEntity? conversation,
        int limit,
        CancellationToken cancellationToken)
    {
        if (conversation == null)
        {
            return new GenerateRequestContext { Inputs = orderedInputs };
        }

        OutputCacheEntity? cache = await dbContext.OutputCaches
            .Where(item => item.OutputContentId == conversation.OutputContentId
                           && item.ModelId == _modelId
                           && item.Provider == strategy.Provider
                           && (item.ExpiredAt == null || item.ExpiredAt >= DateTime.UtcNow))
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (cache == null)
        {
            return new GenerateRequestContext { Inputs = orderedInputs };
        }

        List<ConversationEntity> conversations = await LoadConversationChainAsync(conversation.Id, limit, cancellationToken);
        List<ContentEntity> contents = [];

        foreach (ConversationEntity item in conversations)
        {
            if (item.InputContent != null)
            {
                contents.Add(item.InputContent);
            }

            if (item.OutputContent != null)
            {
                contents.Add(item.OutputContent);
            }
        }

        orderedInputs[0].SetCacheInfo(strategy.Model, new CacheInfo(cache.CreatedAt, cache.ExpiredAt, cache.CacheKey, cache.SequenceIndex));
        orderedInputs[0].SetCachedConversation(conversation.Id);
        orderedInputs[0].SetCacheBreakpoint(cache.SequenceIndex);

        return new GenerateRequestContext
        {
            Inputs = orderedInputs,
            PreviousContents = contents,
            ParentConversation = conversation,
            ReusableCache = cache
        };
    }

    private async Task<List<ConversationEntity>> LoadConversationChainAsync(
        long conversationId,
        int limit,
        CancellationToken cancellationToken)
    {
        List<ConversationEntity> chain = [];
        long? currentId = conversationId;

        while (currentId != null && chain.Count < limit)
        {
            ConversationEntity? conversation = await dbContext.Conversations
                .Include(item => item.InputContent)
                .Include(item => item.OutputContent)
                .FirstOrDefaultAsync(item => item.Id == currentId.Value, cancellationToken);

            if (conversation == null)
            {
                break;
            }

            chain.Add(conversation);
            currentId = conversation.ParentConversationId;
        }

        chain.Reverse();
        return chain;
    }

    private static string HashInput(string content)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
