using Core.Enum;

namespace Core.GenerateAI;

public class GenerateGemini(string modelName, int token = 4096)
    : GenerateOpenAiBase(
        modelName,
        GetApiKey("GEMINI_API_KEY"),
        new Uri("https://generativelanguage.googleapis.com/v1beta/openai/"),
        token,
        Model.Gemini);
