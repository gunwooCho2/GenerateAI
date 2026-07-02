using System.Text.Json;

namespace Core.Dto;

public class ToolEnd
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
    public JsonElement? JsonElement { get; init; }
}
