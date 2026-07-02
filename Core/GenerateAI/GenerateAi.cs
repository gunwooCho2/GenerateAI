using Core.Data;
using Core.Dto;
using Core.Enum;
using Core.Interface;
using Core.JsonSchema;
using NLog;

namespace Core.GenerateAI;

public abstract class GenerateAi(string modelName, string apiKey, int token = 4096)
{
    protected readonly string ModelName = modelName;
    protected readonly string ApiKey = apiKey;
    protected readonly int Token = token;
    protected abstract Model ProviderModel { get; }
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public bool IsWebToolCall = false;

    public abstract Task<GenerateOutput<string>> GenerateAsync(string prompt, List<GenerateInput> inputs);

    public virtual Task<GenerateOutput<string>> GenerateAsync(string prompt, List<GenerateInput> inputs, int limit)
    {
        return GenerateAsync(prompt, inputs);
    }

    public abstract Task<GenerateOutput<string>> GenerateUseToolAsync(string prompt, List<GenerateInput> inputs, List<IToolInfo> tools);

    public virtual async Task<GenerateOutput<string>> GenerateJsonStrAsync(
        string prompt,
        List<GenerateInput> inputs,
        JsonSchemaDto jsonSchemaDto,
        int limit = 20)
    {
        string schema = jsonSchemaDto.GetSchema();
        string jsonPrompt = $"""
                            {prompt}

                            반드시 JSON 객체만 응답해.
                            markdown 코드블록, 설명문, 주석은 출력하지 마라.

                            아래 JSON Schema를 만족하는 JSON을 출력해:

                            {schema}
                            """;

        return await GenerateAsync(jsonPrompt, inputs, limit);
    }

    public virtual async Task<GenerateOutput<T>> GenerateJsonAsync<T>(
        string prompt,
        List<GenerateInput> inputs,
        JsonSchemaDto jsonSchemaDto,
        int limit = 20)
    {
        GenerateOutput<string> output = await GenerateJsonStrAsync(prompt, inputs, jsonSchemaDto, limit);
        T? result = output.Content == null
            ? default
            : System.Text.Json.JsonSerializer.Deserialize<T>(output.Content);

        return new GenerateOutput<T>(
            result,
            output.TotalTokens,
            output.InputTokens,
            output.OutputTokens,
            output.CacheHitTokens,
            output.CacheInfo);
    }

    protected GenerateAiDbContext? CreateDbContext()
    {
        return GenerateAiDbContextFactory.CreateFromEnvironment();
    }

    protected static string GetApiKey(string keyName)
    {
        string? apiKey = Environment.GetEnvironmentVariable(keyName);
        if (apiKey != null) return apiKey;
        string errorMessage = $"{keyName} environment variable is not set";
        Logger.Error(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }
}
