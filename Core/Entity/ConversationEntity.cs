namespace Core.Entity;

public class ConversationEntity
{
    public long Id { get; set; }
    public long? ParentConversationId { get; set; }
    public long InputContentId { get; set; }
    public long OutputContentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ConversationEntity? ParentConversation { get; set; }
    public ContentEntity? InputContent { get; set; }
    public ContentEntity? OutputContent { get; set; }
    public List<ConversationEntity> Children { get; set; } = [];
}
