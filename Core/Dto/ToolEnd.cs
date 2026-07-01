using System.Text.Json;
using System.Text.Json.Serialization;

namespace FreeModel.Dto.ToolOutput;

public class ToolEnd
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;
    
    public static ToolEnd SuccessJson(string json)
    {
        using var doc = JsonDocument.Parse(json); // 유효성 검증

        return new ToolEnd
        {
            IsSuccess = true,
            Message = doc.RootElement.GetRawText()
        };
    }
}