using Core.Enum;

namespace Core.Dto;

public class GenerateOutput<T>(T? content, int totalTokens, int inputTokens, int outputTokens, int cacheHitTokens, CacheInfo? cacheInfo)
{
    public readonly T? Content = content;
    public bool IsSuccessful => Content != null;
    public readonly int TotalTokens = totalTokens;
    public readonly int InputTokens = inputTokens;
    public readonly int OutputTokens = outputTokens;
    public readonly int CacheHitTokens = cacheHitTokens;
    public readonly CacheInfo? CacheInfo = cacheInfo;
}