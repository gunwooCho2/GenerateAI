using Core.Dto;
using Core.Entity;

namespace Core.GenerateAI.Cache;

internal sealed class GenerateRequestContext
{
    public List<GenerateInput> Inputs { get; init; } = [];
    public List<ContentEntity> PreviousContents { get; init; } = [];
    public ConversationEntity? ParentConversation { get; init; }
    public OutputCacheEntity? ReusableCache { get; init; }
}
