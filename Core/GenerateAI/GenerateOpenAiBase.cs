using Core.Data;
using Core.Dto;
using Core.Entity;
using Core.Enum;
using Core.GenerateAI.Cache;
using Core.Interface;
using NLog;
using OpenAI;
using OpenAI.Chat;

namespace Core.GenerateAI;

public class GenerateOpenAiBase : GenerateAi
{
    private readonly ChatClient _client;
    private readonly Model _providerModel;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected override Model ProviderModel => _providerModel;

    protected GenerateOpenAiBase(
        string modelName,
        string apiKey,
        Uri? baseUrl = null,
        int token = 4096,
        Model providerModel = Model.Gpt) : base(modelName, apiKey, token)
    {
        _providerModel = providerModel;
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
        return await GenerateAsync(prompt, inputs, limit: 20);
    }

    public override async Task<GenerateOutput<string>> GenerateAsync(string prompt, List<GenerateInput> inputs, int limit)
    {
        Logger.Trace("GenerateAsync called. InputCount={InputCount}", inputs.Count);

        ProviderCacheStrategy strategy = ProviderCacheStrategies.For(ProviderModel);
        await using GenerateAiDbContext? dbContext = CreateDbContext();
        GenerateCacheStore? cacheStore = dbContext == null ? null : new GenerateCacheStore(dbContext, strategy);
        GenerateRequestContext requestContext = cacheStore == null
            ? new GenerateRequestContext { Inputs = inputs.OrderBy(input => input.Turn).ToList() }
            : await cacheStore.PrepareAsync(inputs, limit);

        List<ChatMessage> messages = BuildMessages(prompt, requestContext);
        ChatCompletion completion = await _client.CompleteChatAsync(messages);

        int inputTokens = completion.Usage.InputTokenCount;
        int outputTokens = completion.Usage.OutputTokenCount;
        int totalTokens = completion.Usage.TotalTokenCount;
        int cacheHitTokens = completion.Usage.InputTokenDetails.CachedTokenCount;

        Logger.Debug(
            "OpenAI compatible token usage. Input={InputTokens}, Output={OutputTokens}, Total={TotalTokens}",
            inputTokens,
            outputTokens,
            totalTokens
        );

        string? output = completion.Content.Count > 0 ? completion.Content[0].Text : null;
        CacheInfo? cacheInfo = null;

        if (output != null && cacheStore != null)
        {
            cacheInfo = await cacheStore.SaveAsync(
                requestContext.Inputs,
                output,
                Guid.NewGuid().ToString("N"),
                strategy.GetNextSequenceIndex(messages.Cast<object>().ToList()),
                requestContext.ParentConversation?.Id);
        }

        return new GenerateOutput<string>(
            output,
            totalTokens,
            inputTokens,
            outputTokens,
            cacheHitTokens,
            cacheInfo);
    }

    public override Task<GenerateOutput<string>> GenerateUseToolAsync(string prompt, List<GenerateInput> inputs, List<IToolInfo> tools)
    {
        throw new NotImplementedException();
    }

    private static List<ChatMessage> BuildMessages(string prompt, GenerateRequestContext context)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(prompt)
        ];

        foreach (ContentEntity content in context.PreviousContents)
        {
            if (content.Role == ContentRole.Input) messages.Add(new UserChatMessage(content.Message));
            else if (content.Role == ContentRole.Output) messages.Add(new AssistantChatMessage(content.Message));
            else if (content.Role == ContentRole.System) messages.Add(new SystemChatMessage(content.Message));
        }

        foreach (GenerateInput input in context.Inputs)
        {
            if (input.Role == Role.Assistant) messages.Add(new AssistantChatMessage(input.Content));
            else if (input.Role == Role.User) messages.Add(new UserChatMessage(input.Content));
            else if (input.Role == Role.System) messages.Add(new SystemChatMessage(input.Content));
            else
            {
                Logger.Error($"Invalid role: {input.Role}");
                throw new InvalidOperationException($"Invalid role: {input.Role}");
            }
        }

        return messages;
    }
}
