#pragma warning disable OPENAI001
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Interface;
using OpenAI.Responses;

namespace Core.Dto;

public class ToolInfo<T>(
    FunctionTool functionTool,
    Func<T, ToolEnd> method) : IToolInfo
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ResponseTool FunctionTool { get; } = functionTool;
    public string FunctionName { get; } = functionTool.FunctionName;

    public ToolEnd Invoke(string argumentsJson)
    {
        T? dto = JsonSerializer.Deserialize<T>(argumentsJson, JsonOptions);

        if (dto == null)
            return new ToolEnd
            {
                IsSuccess = false,
                Message = "Failed to deserialize tool arguments."
            };

        return method(dto);
    }
}
