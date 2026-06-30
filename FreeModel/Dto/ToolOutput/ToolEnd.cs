using System.Text.Json.Serialization;

namespace FreeModel.Dto.ToolOutput;

public class ToolEnd
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;
}