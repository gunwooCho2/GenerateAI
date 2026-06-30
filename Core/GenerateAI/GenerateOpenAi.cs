#pragma warning disable OPENAI001

using System.Text.Json;
using Core.Dto;
using Core.Enum;
using Core.Interface;
using FreeModel.Dto.ToolOutput;
using OpenAI.Responses;

namespace Core.GenerateAI;

public class GenerateOpenAi(string modelName, int token = 4096): GenerateAi(modelName, GetApiKey("OPENAI_API_KEY"), token)
{
    private readonly ResponsesClient _client = new(apiKey: GetApiKey("OPENAI_API_KEY"));
    private const int MaxToolCall = 10;
    public override async Task<GenerateOutput<string>> GenerateAsync(string prompt, List<GenerateInput> inputs)
    {
        CreateResponseOptions options = new CreateResponseOptions
        {
            Model = ModelName,
            Instructions = prompt,
            MaxOutputTokenCount = Token
        };
        ResponseResult response = await GetResponse(options, inputs);
        return GetGenerateOutput<string>(response);
    }

    public override async Task<GenerateOutput<string>> GenerateUseToolAsync(string prompt, List<GenerateInput> inputs, List<IToolInfo> tools)
    {
        var toolMap = tools.ToDictionary(x => x.FunctionName);
        
        int totalToken = 0;
        int inputToken = 0;
        int outputToken = 0;
        int cacheHitToken = 0;
        
        CreateResponseOptions options = new CreateResponseOptions
        {
            Model = ModelName,
            Instructions = prompt,
            MaxOutputTokenCount = Token
        };
        foreach (var tool in tools)
            options.Tools.Add(tool.FunctionTool);
        
        ResponseResult response = await GetResponse(options, inputs);
        int toolCallCount = 0;
        
        while (true)
        {
            options.PreviousResponseId = response.Id;
            options.InputItems.Clear();
            bool hasToolCall = false;

            foreach (ResponseItem item in response.OutputItems)
            {
                if (item is not FunctionCallResponseItem functionCall)
                    continue;

                hasToolCall = true;

                if (!toolMap.TryGetValue(functionCall.FunctionName, out var toolInfo))
                {
                    options.InputItems.Add(new FunctionCallOutputResponseItem(
                        functionCall.CallId,
                        JsonSerializer.Serialize(
                            new ToolEnd
                            {
                                IsSuccess = false,
                                Message = $"Unknown tool {functionCall.FunctionName}"
                            })));
                    continue;
                }

                ToolEnd toolResult;

                try
                {
                    toolResult = toolInfo.Invoke(functionCall.FunctionArguments.ToString());
                }
                catch (Exception ex)
                {
                    toolResult = new ToolEnd
                    {
                        IsSuccess = false,
                        Message = ex.Message
                    };
                }

                options.InputItems.Add(new FunctionCallOutputResponseItem(
                    functionCall.CallId,
                    JsonSerializer.Serialize(toolResult)));
            }

            if (!hasToolCall)
                return GetGenerateOutput<string>(response, totalToken, inputToken, outputToken, cacheHitToken);
            
            toolCallCount++;
            totalToken += response.Usage.TotalTokenCount;
            inputToken += response.Usage.InputTokenCount;
            outputToken += response.Usage.OutputTokenCount;
            cacheHitToken += response.Usage.InputTokenDetails.CachedTokenCount;
            
            if (toolCallCount > MaxToolCall)
                throw new Exception("Tool call loop exceeded.");
            
            response = await _client.CreateResponseAsync(options);
        }
    }

    private async Task<ResponseResult> GetResponse(CreateResponseOptions options, List<GenerateInput> inputs)
    {
        var orderedInputs = inputs.OrderBy(x => x.Turn).ToList();
        int cachedIndex = CachedIndex(orderedInputs);
        
        if (cachedIndex != -1)
        {
            var lastCachedInput = orderedInputs[cachedIndex];
            options.PreviousResponseId = lastCachedInput.CacheInfos[Model.Gpt].CacheKey;
        }

        BuildMessages(options, orderedInputs, cachedIndex); 
        return await _client.CreateResponseAsync(options);
    }

    private int CachedIndex(List<GenerateInput> orderedInputs)
    {
        int firstInvalidIndex = orderedInputs.FindIndex(x =>
            !x.CacheInfos.TryGetValue(Model.Gpt, out var cacheInfo)
            || !cacheInfo.IsUsable);

        return firstInvalidIndex != -1
            ? firstInvalidIndex - 1
            : orderedInputs.Count - 1;
    }
    
    private static void BuildMessages(CreateResponseOptions options, List<GenerateInput> orderedInputs, int cachedIndex)
    {
        for (int i = cachedIndex + 1; i < orderedInputs.Count; i++)
        {
            var input = orderedInputs[i];
            var requestMessage = input.Role == Role.User ? ResponseItem.CreateUserMessageItem(input.Content) : ResponseItem.CreateAssistantMessageItem(input.Content);
            options.InputItems.Add(requestMessage);
        }
    }
    
    private static GenerateOutput<T> GetGenerateOutput<T>(ResponseResult response, int previousTokenCount = 0, int inputTokenCount = 0, int outputTokenCount = 0, int cacheHitTokenCount = 0)
    {
        int totalToken = response.Usage.TotalTokenCount;
        int inputToken = response.Usage.InputTokenCount;
        int outputToken = response.Usage.OutputTokenCount;
        int cacheHitToken = response.Usage.InputTokenDetails.CachedTokenCount;

        var responseId = response.Id;
        var dateNow = DateTime.UtcNow;
        var cacheInfo = new CacheInfo(dateNow, dateNow.AddDays(30), responseId);

        T output;

        if (typeof(T) == typeof(string))
        {
            output = (T)(object)response.GetOutputText();
        }
        else
        {
            throw new NotImplementedException($"Type {typeof(T).Name} is not supported.");
        }

        return new GenerateOutput<T>(
            output,
            totalToken,
            inputToken,
            outputToken,
            cacheHitToken,
            cacheInfo
        );
    }
}