using Core.Enum;

namespace Core.GenerateAI.Cache;

internal static class ProviderCacheStrategies
{
    public static ProviderCacheStrategy For(Model model)
    {
        return model switch
        {
            Model.Gpt => new GptCacheStrategy(),
            Model.Gemini => new GeminiCacheStrategy(),
            Model.Claude => new ClaudeCacheStrategy(),
            Model.Grok => new GrokCacheStrategy(),
            Model.Deepseek => new DeepseekCacheStrategy(),
            _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
        };
    }
}
