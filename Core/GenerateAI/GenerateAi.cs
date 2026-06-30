using Core.Dto;
using Core.Interface;
using NLog;

namespace Core.GenerateAI;

public abstract class GenerateAi(string modelName, string apiKey, int token = 4096)
{
    protected readonly string ModelName = modelName;
    protected readonly string ApiKey = apiKey;
    protected readonly int Token = token;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public abstract Task<GenerateOutput<string>> GenerateAsync(string prompt, List<GenerateInput> inputs);
    public abstract Task<GenerateOutput<string>> GenerateUseToolAsync(string prompt, List<GenerateInput> inputs, List<IToolInfo> tools);
    
    protected static string GetApiKey(string keyName)
    {
        string? apiKey = Environment.GetEnvironmentVariable(keyName);
        if (apiKey != null) return apiKey;
        string errorMessage = $"{keyName} environment variable is not set";
        Logger.Error(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }
}