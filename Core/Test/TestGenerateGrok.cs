using Core.Dto;
using Core.Enum;
using Core.GenerateAI;

namespace Core.Test;

public static class TestGenerateGrok
{
    private static GenerateGrok CreateWithPromptCacheKey()
    {
        return new GenerateGrok(
            modelName: "grok-4",
            promptCacheKey: "conversation-1234");
    }

    public static async Task<GenerateOutput<string>> RunAsync()
    {
        GenerateGrok grok = CreateWithPromptCacheKey();
        List<GenerateInput> inputs =
        [
            new GenerateInput(Role.User, "간단히 자기소개해줘.", 1)
        ];

        return await grok.GenerateAsync("너는 간결하게 답하는 AI야.", inputs);
    }
}
