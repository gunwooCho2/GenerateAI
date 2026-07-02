namespace Core.Dto;

public class CacheInfo(DateTime cachedAt, DateTime? expiresAt, string cacheKey, int? sequenceIndex = null)
{
    public bool IsUsable => (ExpiresAt == null || ExpiresAt >= DateTime.UtcNow) && IsCached;
    public bool IsCached { get; internal set; } = true;
    public DateTime CachedAt { get; internal set; } = cachedAt;
    public DateTime? ExpiresAt { get; internal set; } = expiresAt;
    public string CacheKey { get; internal set; } = cacheKey;
    public int? SequenceIndex { get; internal set; } = sequenceIndex;
}
