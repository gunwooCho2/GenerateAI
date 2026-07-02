using Core.Enum;

namespace Core.Dto;

public class GenerateInput(Role role, string content, int turn)
{
    public Role Role { get; init; } = role;
    public string Content { get; private set; } = content;
    internal Dictionary<Model, CacheInfo> CacheInfos { get; } = new();
    internal long? CachedConversationId { get; private set; }
    internal int? CacheBreakpointSequenceIndex { get; private set; }
    public readonly int Turn = turn;

    public void SetContent(string content)
    {
        if (Content == content)
        {
            return;
        }

        Content = content;
        foreach (CacheInfo value in CacheInfos.Values)
        {
            value.IsCached = false;
        }
    }

    internal void SetCacheInfo(Model model, CacheInfo cacheInfo)
    {
        CacheInfos[model] = cacheInfo;
    }

    internal void SetCachedConversation(long conversationId)
    {
        CachedConversationId = conversationId;
    }

    internal void SetCacheBreakpoint(int? sequenceIndex)
    {
        CacheBreakpointSequenceIndex = sequenceIndex;
    }
}
