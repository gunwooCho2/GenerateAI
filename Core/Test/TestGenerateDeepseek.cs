using Core.Dto;
using Core.GenerateAI;

namespace Core.Test;

public static class TestGenerateDeepseek
{
    private static GenerateDeepseek Create()
    {
        return new GenerateDeepseek("deepseek-chat");
    }

    public static async Task<GenerateOutput<string>> RunGenerateAsync()
    {
        GenerateDeepseek generateAi = Create();
        return await generateAi.GenerateAsync(TestGenerateAiSamples.Prompt, TestGenerateAiSamples.CreateInputs());
    }

    public static async Task<GenerateOutput<string>> RunGenerateWithLimitAsync()
    {
        GenerateDeepseek generateAi = Create();
        return await generateAi.GenerateAsync(TestGenerateAiSamples.Prompt, TestGenerateAiSamples.CreateInputs(), limit: 5);
    }

    public static async Task<NotImplementedException> RunGenerateUseToolThrowsAsync()
    {
        return await TestGenerateAiSamples.RunWithTemporaryEnvironmentVariableAsync(
            "DEEP_SEEK_API",
            "test-deepseek-api-key",
            async () =>
        {
            GenerateDeepseek generateAi = Create();

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
        GenerateDeepseek generateAi = Create();
        return await generateAi.GenerateJsonStrAsync(
            TestGenerateAiSamples.JsonPrompt,
            TestGenerateAiSamples.CreateInputs(),
            TestGenerateAiSamples.CreateJsonSchema());
    }

    public static async Task<GenerateOutput<TestGenerateAiSamples.JsonTestDto>> RunGenerateJsonAsync()
    {
        GenerateDeepseek generateAi = Create();
        return await generateAi.GenerateJsonAsync<TestGenerateAiSamples.JsonTestDto>(
            TestGenerateAiSamples.JsonPrompt,
            TestGenerateAiSamples.CreateInputs(),
            TestGenerateAiSamples.CreateJsonSchema());
    }
}
