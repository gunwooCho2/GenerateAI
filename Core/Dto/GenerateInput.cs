using Core.Enum;

namespace Core.Dto;

public class GenerateInput(Role role, string content)
{
    public Role Role { get; init; } = role;
    public string Content { get; private set; } = content;
    public Dictionary<Model, CacheInfo> CacheInfos { get; } = new();

    public void SetContent(string content)
    {
        if (Content == content)
            return;

        Content = content;

        foreach (var value in CacheInfos.Values)
        {
            value.IsCached = false;
        }
    }
}