using Core.Enum;

namespace Core.GenerateAI.Cache;

internal abstract class ProviderCacheStrategy
{
    public abstract Model Model { get; }
    public abstract CacheProvider Provider { get; }
    public virtual CacheType CacheType => CacheType.ProviderResponse;

    public virtual int? GetNextSequenceIndex(IReadOnlyList<object> contentBlocks)
    {
        return null;
    }
}

internal sealed class GptCacheStrategy : ProviderCacheStrategy
{
    public override Model Model => Model.Gpt;
    public override CacheProvider Provider => CacheProvider.Gpt;
}

internal sealed class DeepseekCacheStrategy : ProviderCacheStrategy
{
    public override Model Model => Model.Deepseek;
    public override CacheProvider Provider => CacheProvider.Deepseek;
}

internal sealed class GeminiCacheStrategy : ProviderCacheStrategy
{
    public override Model Model => Model.Gemini;
    public override CacheProvider Provider => CacheProvider.Gemini;
}

internal sealed class GrokCacheStrategy : ProviderCacheStrategy
{
    public override Model Model => Model.Grok;
    public override CacheProvider Provider => CacheProvider.Grok;
}

internal sealed class ClaudeCacheStrategy : ProviderCacheStrategy
{
    public override Model Model => Model.Claude;
    public override CacheProvider Provider => CacheProvider.Claude;
    public override CacheType CacheType => CacheType.PromptBreakpoint;

    public override int? GetNextSequenceIndex(IReadOnlyList<object> contentBlocks)
    {
        if (contentBlocks.Count == 0)
        {
            return null;
        }

        return contentBlocks.Count - 1;
    }
}
