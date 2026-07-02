using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Data;
using Core.Dto;
using Core.Entity;
using Core.Enum;
using Core.GenerateAI.Cache;
using Core.Interface;
using NLog;

namespace Core.GenerateAI;

public class GenerateClaude(string modelName, int token = 4096)
    : GenerateAi(modelName, GetApiKey("ANTHROPIC_API_KEY"), token), IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://api.anthropic.com/v1/")
    };

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected override Model ProviderModel => Model.Claude;

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

        List<ClaudeMessage> messages = BuildMessages(requestContext);
        ApplyClaudeBreakpoint(messages, requestContext.ReusableCache?.SequenceIndex);

        ClaudeRequest request = new()
        {
            Model = ModelName,
            MaxTokens = Token,
            System = prompt,
            Messages = messages
        };

        ClaudeResponse response = await SendAsync(request);
        string output = ExtractText(response);
        CacheInfo? cacheInfo = null;

        if (cacheStore != null)
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
            response.Usage?.InputTokens + response.Usage?.OutputTokens ?? 0,
            response.Usage?.InputTokens ?? 0,
            response.Usage?.OutputTokens ?? 0,
            response.Usage?.CacheReadInputTokens ?? 0,
            cacheInfo);
    }

    public override Task<GenerateOutput<string>> GenerateUseToolAsync(string prompt, List<GenerateInput> inputs, List<IToolInfo> tools)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string prompt,
        List<GenerateInput> inputs,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ClaudeRequest request = new()
        {
            Model = ModelName,
            MaxTokens = Token,
            System = prompt,
            Messages = BuildMessages(new GenerateRequestContext
            {
                Inputs = inputs.OrderBy(input => input.Turn).ToList()
            }),
            Stream = true
        };

        using HttpRequestMessage httpRequest = CreateRequest(request);
        using HttpResponseMessage response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using StreamReader reader = new(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line == null)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
            {
                continue;
            }

            string data = line["data: ".Length..];
            if (data == "[DONE]")
            {
                yield break;
            }

            using JsonDocument doc = JsonDocument.Parse(data);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("type", out JsonElement typeElement)
                && typeElement.GetString() == "content_block_delta"
                && root.TryGetProperty("delta", out JsonElement delta)
                && delta.TryGetProperty("text", out JsonElement textElement)
                && !string.IsNullOrEmpty(textElement.GetString()))
            {
                yield return textElement.GetString()!;
            }
        }
    }

    private async Task<ClaudeResponse> SendAsync(ClaudeRequest request)
    {
        using HttpRequestMessage httpRequest = CreateRequest(request);
        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest);
        string responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Logger.Error($"Claude API error: {response.StatusCode} / {responseText}");
            throw new HttpRequestException($"Claude API error: {response.StatusCode} / {responseText}");
        }

        ClaudeResponse? result = JsonSerializer.Deserialize<ClaudeResponse>(responseText, JsonOptions);
        return result ?? throw new InvalidOperationException("Failed to deserialize Claude response.");
    }

    private HttpRequestMessage CreateRequest(ClaudeRequest request)
    {
        string json = JsonSerializer.Serialize(request, JsonOptions);

        HttpRequestMessage httpRequest = new(HttpMethod.Post, "messages")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Add("x-api-key", ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return httpRequest;
    }

    private static List<ClaudeMessage> BuildMessages(GenerateRequestContext context)
    {
        List<ClaudeMessage> messages = [];

        foreach (ContentEntity content in context.PreviousContents)
        {
            string role = content.Role switch
            {
                ContentRole.Input => "user",
                ContentRole.Output => "assistant",
                _ => "user"
            };

            messages.Add(new ClaudeMessage
            {
                Role = role,
                Content =
                [
                    new ClaudeContentBlock
                    {
                        Type = "text",
                        Text = content.Message
                    }
                ]
            });
        }

        foreach (GenerateInput input in context.Inputs)
        {
            string role = input.Role switch
            {
                Role.User => "user",
                Role.Assistant => "assistant",
                Role.System => "user",
                _ => throw new InvalidOperationException($"Invalid role: {input.Role}")
            };

            messages.Add(new ClaudeMessage
            {
                Role = role,
                Content =
                [
                    new ClaudeContentBlock
                    {
                        Type = "text",
                        Text = input.Content
                    }
                ]
            });
        }

        return messages;
    }

    private static void ApplyClaudeBreakpoint(List<ClaudeMessage> messages, int? sequenceIndex)
    {
        if (messages.Count == 0)
        {
            return;
        }

        int index = sequenceIndex is >= 0 && sequenceIndex < messages.Count
            ? sequenceIndex.Value
            : messages.Count - 1;

        ClaudeContentBlock? block = messages[index].Content.LastOrDefault();
        if (block != null && !string.IsNullOrWhiteSpace(block.Text))
        {
            block.CacheControl = new ClaudeCacheControl { Type = "ephemeral" };
        }
    }

    private static string ExtractText(ClaudeResponse response)
    {
        return string.Concat(
            response.Content
                .Where(block => block.Type == "text" && !string.IsNullOrEmpty(block.Text))
                .Select(block => block.Text));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class ClaudeRequest
    {
        public required string Model { get; init; }
        public int MaxTokens { get; init; }
        public string? System { get; init; }
        public required List<ClaudeMessage> Messages { get; init; }
        public bool? Stream { get; init; }
    }

    private sealed class ClaudeMessage
    {
        public required string Role { get; init; }
        public required List<ClaudeContentBlock> Content { get; init; }
    }

    private sealed class ClaudeContentBlock
    {
        public required string Type { get; init; }
        public string? Text { get; init; }
        public ClaudeCacheControl? CacheControl { get; set; }
    }

    private sealed class ClaudeCacheControl
    {
        public required string Type { get; init; }
    }

    private sealed class ClaudeResponse
    {
        public List<ClaudeContentBlock> Content { get; init; } = [];
        public ClaudeUsage? Usage { get; init; }
    }

    private sealed class ClaudeUsage
    {
        public int InputTokens { get; init; }
        public int OutputTokens { get; init; }
        public int CacheReadInputTokens { get; init; }
        public int CacheCreationInputTokens { get; init; }
    }
}
