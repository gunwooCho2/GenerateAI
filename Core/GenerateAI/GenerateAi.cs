using Core.Dto;
using NLog;

namespace Core.GenerateAI;

public abstract class GenerateAi(string modelName, string apiKey, int token = 4096)
{
    protected readonly string ModelName = modelName;
    protected readonly string ApiKey = apiKey;
    protected readonly int Token = token;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public abstract Task<string> GenerateAsync(string prompt, List<GenerateInput> inputs);
    
    protected static string GetApiKey(string keyName)
    {
        string? apiKey = Environment.GetEnvironmentVariable(keyName);
        if (apiKey != null) return apiKey;
        string errorMessage = $"{keyName} environment variable is not set";
        Logger.Error(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }
}