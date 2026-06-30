using Core.Dto;
using Core.Enum;
using Core.Interface;
using NLog;
using OpenAI;
using OpenAI.Chat;

namespace Core.GenerateAI;

public class GenerateOpenAiBase:GenerateAi
{
    private readonly ChatClient _client;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    protected GenerateOpenAiBase(string modelName, string apiKey, Uri? baseUrl=null, int token = 4096) : base(modelName, apiKey, token)
    {
        Logger.Trace("Initialize GenerateOpenAiBase");
        OpenAIClientOptions options = new();
        
        if (baseUrl != null)
        {
            options.Endpoint = baseUrl;
        }

        OpenAIClient openAiClient = new(
            credential: new System.ClientModel.ApiKeyCredential(apiKey),
            options: options
        );

        _client = openAiClient.GetChatClient(modelName);
    }
    
    public override async Task<GenerateOutput<string>> GenerateAsync(string prompt, List<GenerateInput> inputs)
    {
        Logger.Trace("GenerateAsync called. InputCount={InputCount}", inputs.Count);
        List<ChatMessage> messages = BuildMessages(prompt, inputs);
        inputs[^1].CacheInfos.TryGetValue(Model.Gpt, out var lastCacheInfo);
        ChatCompletion completion = await _client.CompleteChatAsync(messages);
        
        int inputTokens = completion.Usage.InputTokenCount;
        int outputTokens = completion.Usage.OutputTokenCount;
        int totalTokens = completion.Usage.TotalTokenCount;
        int cacheHitTokens = completion.Usage.InputTokenDetails.CachedTokenCount;
        
        Logger.Debug(
            "OpenAI token usage. Input={InputTokens}, Output={OutputTokens}, Total={TotalTokens}",
            inputTokens,
            outputTokens,
            totalTokens
        );

        if (completion.Content.Count > 0)
        {
            Logger.Trace("GenerateAsync completed. ContentCount={ContentCount}", completion.Content.Count);
            return new GenerateOutput<string>(completion.Content[0].Text, totalTokens, inputTokens, cacheHitTokens, outputTokens, lastCacheInfo);
        }

        Logger.Warn("No content returned from OpenAI API");
        return new GenerateOutput<string>(null, totalTokens, inputTokens, outputTokens, cacheHitTokens, lastCacheInfo);
    }

    public override async Task<GenerateOutput<string>> GenerateUseToolAsync(string prompt, List<GenerateInput> inputs, List<IToolInfo> tools)
    {
        throw new NotImplementedException();
    }

    private static List<ChatMessage> BuildMessages(string prompt, List<GenerateInput> inputs)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(prompt)
        ];

        foreach (GenerateInput input in inputs)
        {
            if (input.Role == Role.Assistant) messages.Add(new AssistantChatMessage(input.Content));
            else if (input.Role == Role.User) messages.Add(new UserChatMessage(input.Content));
            else
            {
                Logger.Error($"Invalid role: {input.Role}");
                throw new InvalidOperationException($"Invalid role: {input.Role}");
            }
        }
        return messages;
    }
}