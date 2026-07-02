using Core.Enum;

namespace Core.Entity;

public class ContentEntity
{
    public long Id { get; set; }
    public ContentRole Role { get; set; }
    public required string Message { get; set; }
    public string? Hash { get; set; }
    public int ModelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ModelEntity? Model { get; set; }
}
