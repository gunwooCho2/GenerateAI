using Core.Enum;

namespace Core.Entity;

public class OutputCacheEntity
{
    public long Id { get; set; }
    public long OutputContentId { get; set; }
    public int ModelId { get; set; }
    public CacheProvider Provider { get; set; }
    public required string CacheKey { get; set; }
    public CacheType CacheType { get; set; }
    public int? SequenceIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiredAt { get; set; }

    public ContentEntity? OutputContent { get; set; }
    public ModelEntity? Model { get; set; }
}
