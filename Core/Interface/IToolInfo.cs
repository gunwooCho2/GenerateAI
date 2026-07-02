#pragma warning disable OPENAI001
using Core.Dto;
using OpenAI.Responses;

namespace Core.Interface;

public interface IToolInfo
{
    ResponseTool FunctionTool { get; }
    string FunctionName { get; }
    ToolEnd Invoke(string argumentsJson);
}