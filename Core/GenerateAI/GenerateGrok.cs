using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Data;
using Core.Dto;
using Core.Entity;
using Core.Enum;
using Core.GenerateAI.Cache;
using Core.Interface;

namespace Core.GenerateAI;

public class GenerateGrok : GenerateAi, IDisposable
{
    private readonly string? _promptCacheKey;
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://api.x.ai/v1/")
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected override Model ProviderModel => Model.Grok;

    public GenerateGrok(string modelName, int token = 4096, string? promptCacheKey = null)
        : base(modelName, GetApiKey("GROK_API_KEY"), token)
    {
        _promptCacheKey = promptCacheKey;
    }

    public override async Task<GenerateOutput<string>> GenerateAsync(string prompt, List<GenerateInput> inputs)
    {
        return await GenerateAsync(prompt, inputs, limit: 20);
    }

    public override async Task<GenerateOutput<string>> GenerateAsync(string prompt, List<GenerateInput> inputs, int limit)
    {
        ProviderCacheStrategy strategy = ProviderCacheStrategies.For(ProviderModel);
        await using GenerateAiDbContext? dbContext = CreateDbContext();
        GenerateCacheStore? cacheStore = dbContext == null ? null : new GenerateCacheStore(dbContext, strategy);
        GenerateRequestContext requestContext = cacheStore == null
            ? new GenerateRequestContext { Inputs = inputs.OrderBy(input => input.Turn).ToList() }
            : await cacheStore.PrepareAsync(inputs, limit);

        string input = BuildInput(prompt, requestContext);
        GrokResponse response = await SendAsync(new GrokRequest
        {
            Model = ModelName,
            Input = input,
            PromptCacheKey = _promptCacheKey
        });

        string output = ExtractText(response);
        CacheInfo? cacheInfo = null;

        if (cacheStore != null)
        {
            cacheInfo = await cacheStore.SaveAsync(
                requestContext.Inputs,
                output,
                _promptCacheKey ?? Guid.NewGuid().ToString("N"),
                sequenceIndex: null,
                requestContext.ParentConversation?.Id);
        }

        return new GenerateOutput<string>(
            output,
            response.Usage?.TotalTokens ?? 0,
            response.Usage?.InputTokens ?? 0,
            response.Usage?.OutputTokens ?? 0,
            response.Usage?.CachedInputTokens ?? 0,
            cacheInfo);
    }

    public override Task<GenerateOutput<string>> GenerateUseToolAsync(string prompt, List<GenerateInput> inputs, List<IToolInfo> tools)
    {
        throw new NotImplementedException();
    }

    private async Task<GrokResponse> SendAsync(GrokRequest request)
    {
        string json = JsonSerializer.Serialize(request, JsonOptions);
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, "responses")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Grok API error: {response.StatusCode} / {responseText}");
        }

        return JsonSerializer.Deserialize<GrokResponse>(responseText, JsonOptions)
               ?? throw new InvalidOperationException("Failed to deserialize Grok response.");
    }

    private static string BuildInput(string prompt, GenerateRequestContext context)
    {
        StringBuilder builder = new();
        builder.AppendLine(prompt);

        foreach (ContentEntity content in context.PreviousContents)
        {
            builder.Append(content.Role).Append(": ").AppendLine(content.Message);
        }

        foreach (GenerateInput input in context.Inputs)
        {
            builder.Append(input.Role).Append(": ").AppendLine(input.Content);
        }

        return builder.ToString();
    }

    private static string ExtractText(GrokResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.OutputText))
        {
            return response.OutputText;
        }

        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            return response.Text;
        }

        return string.Concat(
            response.Output
                .SelectMany(output => output.Content)
                .Where(content => !string.IsNullOrWhiteSpace(content.Text))
                .Select(content => content.Text));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class GrokRequest
    {
        public required string Model { get; init; }
        public required string Input { get; init; }
        public string? PromptCacheKey { get; init; }
    }

    private sealed class GrokResponse
    {
        public string? OutputText { get; init; }
        public string? Text { get; init; }
        public List<GrokOutput> Output { get; init; } = [];
        public GrokUsage? Usage { get; init; }
    }

    private sealed class GrokOutput
    {
        public List<GrokContent> Content { get; init; } = [];
    }

    private sealed class GrokContent
    {
        public string? Text { get; init; }
    }

    private sealed class GrokUsage
    {
        public int InputTokens { get; init; }
        public int OutputTokens { get; init; }
        public int TotalTokens { get; init; }
        public int CachedInputTokens { get; init; }
    }
}
