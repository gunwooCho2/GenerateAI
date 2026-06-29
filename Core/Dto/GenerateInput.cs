using Core.Enum;

namespace Core.Dto;

public class GenerateInput(Role role, string content, int turn, Dictionary<Model, CacheInfo>? cacheInfos)
{
    public Role Role { get; init; } = role;
    public string Content { get; private set; } = content;
    public Dictionary<Model, CacheInfo> CacheInfos { get; } = cacheInfos ?? new();
    public readonly int Turn = turn;

    public void SetContent(string content)
    {
        if (Content == content)
            return;

        Content = content;
        foreach (var value in CacheInfos.Values)
        {
            value.IsCached  = false;
        }
    }
}