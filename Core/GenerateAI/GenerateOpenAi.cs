#pragma warning disable OPENAI001

using System.Text.Json;
using Core.Data;
using Core.Dto;
using Core.Enum;
using Core.GenerateAI.Cache;
using Core.Interface;
using OpenAI.Responses;

namespace Core.GenerateAI;

public class GenerateOpenAi(string modelName, int token = 4096): GenerateAi(modelName, GetApiKey("OPENAI_API_KEY"), token)
{
    private readonly ResponsesClient _client = new(apiKey: GetApiKey("OPENAI_API_KEY"));
    private const int MaxToolCall = 10;
    protected override Model ProviderModel => Model.Gpt;
    
    public override async Task<GenerateOutput<string>> GenerateAsync(string prompt, List<GenerateInput> inputs)
    {
        return await GenerateAsync(prompt, inputs, limit: 20);
    }

    public override async Task<GenerateOutput<string>> GenerateAsync(string prompt, List<GenerateInput> inputs, int limit)
    {
        CreateResponseOptions options = new CreateResponseOptions
        {
            Model = ModelName,
            Instructions = prompt,
            MaxOutputTokenCount = Token,
            TruncationMode = ResponseTruncationMode.Auto
        };

        ProviderCacheStrategy strategy = ProviderCacheStrategies.For(ProviderModel);
        await using GenerateAiDbContext? dbContext = CreateDbContext();
        GenerateCacheStore? cacheStore = dbContext == null ? null : new GenerateCacheStore(dbContext, strategy);
        GenerateRequestContext requestContext = cacheStore == null
            ? new GenerateRequestContext { Inputs = inputs.OrderBy(input => input.Turn).ToList() }
            : await cacheStore.PrepareAsync(inputs, limit);

        ResponseResult response = await GetResponse(options, requestContext);
        return await GetGenerateOutput<string>(response, cacheStore, requestContext, strategy);
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
            MaxOutputTokenCount = Token,
            TruncationMode = ResponseTruncationMode.Auto
        };
        foreach (var tool in tools)
            options.Tools.Add(tool.FunctionTool);
        
        if (IsWebToolCall)
            options.Tools.Add(ResponseTool.CreateWebSearchTool());
        
        ProviderCacheStrategy strategy = ProviderCacheStrategies.For(ProviderModel);
        await using GenerateAiDbContext? dbContext = CreateDbContext();
        GenerateCacheStore? cacheStore = dbContext == null ? null : new GenerateCacheStore(dbContext, strategy);
        GenerateRequestContext requestContext = cacheStore == null
            ? new GenerateRequestContext { Inputs = inputs.OrderBy(input => input.Turn).ToList() }
            : await cacheStore.PrepareAsync(inputs, limit: 20);

        ResponseResult response = await GetResponse(options, requestContext);
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
                return await GetGenerateOutput<string>(response, cacheStore, requestContext, strategy, totalToken, inputToken, outputToken, cacheHitToken);
            
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

    private async Task<ResponseResult> GetResponse(CreateResponseOptions options, GenerateRequestContext requestContext)
    {
        if (requestContext.ReusableCache != null && ProviderModel == Model.Gpt)
        {
            options.PreviousResponseId = requestContext.ReusableCache.CacheKey;
        }

        BuildMessages(options, requestContext); 
        return await _client.CreateResponseAsync(options);
    }
    
    private static void BuildMessages(CreateResponseOptions options, GenerateRequestContext requestContext)
    {
        foreach (var content in requestContext.PreviousContents)
        {
            if (content.Role == ContentRole.Input) options.InputItems.Add(ResponseItem.CreateUserMessageItem(content.Message));
            else if (content.Role == ContentRole.Output) options.InputItems.Add(ResponseItem.CreateAssistantMessageItem(content.Message));
        }

        foreach (GenerateInput input in requestContext.Inputs)
        {
            var requestMessage = input.Role == Role.User ? ResponseItem.CreateUserMessageItem(input.Content) : ResponseItem.CreateAssistantMessageItem(input.Content);
            options.InputItems.Add(requestMessage);
        }
    }
    
    private static async Task<GenerateOutput<T>> GetGenerateOutput<T>(
        ResponseResult response,
        GenerateCacheStore? cacheStore,
        GenerateRequestContext requestContext,
        ProviderCacheStrategy strategy,
        int previousTokenCount = 0,
        int inputTokenCount = 0,
        int outputTokenCount = 0,
        int cacheHitTokenCount = 0)
    {
        int totalToken = response.Usage.TotalTokenCount + previousTokenCount;
        int inputToken = response.Usage.InputTokenCount + inputTokenCount;
        int outputToken = response.Usage.OutputTokenCount + outputTokenCount;
        int cacheHitToken = response.Usage.InputTokenDetails.CachedTokenCount + cacheHitTokenCount;

        T output;

        if (typeof(T) == typeof(string))
        {
            output = (T)(object)response.GetOutputText();
        }
        else
        {
            throw new NotImplementedException($"Type {typeof(T).Name} is not supported.");
        }

        CacheInfo? cacheInfo = null;
        if (cacheStore != null && output is string outputText)
        {
            cacheInfo = await cacheStore.SaveAsync(
                requestContext.Inputs,
                outputText,
                response.Id,
                strategy.GetNextSequenceIndex(response.OutputItems.Cast<object>().ToList()),
                requestContext.ParentConversation?.Id);
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
