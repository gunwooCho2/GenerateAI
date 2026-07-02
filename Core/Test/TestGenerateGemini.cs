using Core.Dto;
using Core.GenerateAI;

namespace Core.Test;

public static class TestGenerateGemini
{
    private static GenerateGemini Create()
    {
        return new GenerateGemini("gemini-2.5-flash");
    }

    public static async Task<GenerateOutput<string>> RunGenerateAsync()
    {
        GenerateGemini generateAi = Create();
        return await generateAi.GenerateAsync(TestGenerateAiSamples.Prompt, TestGenerateAiSamples.CreateInputs());
    }

    public static async Task<GenerateOutput<string>> RunGenerateWithLimitAsync()
    {
        GenerateGemini generateAi = Create();
        return await generateAi.GenerateAsync(TestGenerateAiSamples.Prompt, TestGenerateAiSamples.CreateInputs(), limit: 5);
    }

    public static async Task<NotImplementedException> RunGenerateUseToolThrowsAsync()
    {
        return await TestGenerateAiSamples.RunWithTemporaryEnvironmentVariableAsync(
            "GEMINI_API_KEY",
            "test-gemini-api-key",
            async () =>
        {
            GenerateGemini generateAi = Create();

            try
            {
                await generateAi.GenerateUseToolAsync(
                    TestGenerateAiSamples.Prompt,
                    TestGenerateAiSamples.CreateInputs(),
                    TestGenerateAiSamples.CreateNoTools());
            }
            catch (NotImplementedException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("GenerateUseToolAsync should throw NotImplementedException.");
        });
    }

    public static async Task<GenerateOutput<string>> RunGenerateJsonStrAsync()
    {
        GenerateGemini generateAi = Create();
        return await generateAi.GenerateJsonStrAsync(
            TestGenerateAiSamples.JsonPrompt,
            TestGenerateAiSamples.CreateInputs(),
            TestGenerateAiSamples.CreateJsonSchema());
    }

    public static async Task<GenerateOutput<TestGenerateAiSamples.JsonTestDto>> RunGenerateJsonAsync()
    {
        GenerateGemini generateAi = Create();
        return await generateAi.GenerateJsonAsync<TestGenerateAiSamples.JsonTestDto>(
            TestGenerateAiSamples.JsonPrompt,
            TestGenerateAiSamples.CreateInputs(),
            TestGenerateAiSamples.CreateJsonSchema());
    }
}
