using Core.Enum;

namespace Core.GenerateAI;

public class GenerateDeepseek(string modelName, int token = 4096)
    : GenerateOpenAiBase(modelName, GetApiKey("DEEP_SEEK_API"), new Uri("https://api.deepseek.com"), token, Model.Deepseek);
