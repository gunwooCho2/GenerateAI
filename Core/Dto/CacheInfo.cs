namespace Core.Dto;

public class CacheInfo(DateTime cachedAt, DateTime expiresAt, string cacheKey)
{
    public bool IsUsable
    {
        get
        {
            if (ExpiresAt >= DateTime.UtcNow && IsCached) return true;
            return false;
        }
    }
    public bool IsCached { get; set; } = true;
    public DateTime CachedAt { get; set; } = cachedAt;
    public DateTime ExpiresAt { get; set; } = expiresAt;
    public string CacheKey { get; set; } = cacheKey;
}