using Core.Dto;
using Core.GenerateAI;

namespace Core.Test;

public static class TestGenerateClaude
{
    private static GenerateClaude Create()
    {
        return new GenerateClaude("claude-3-5-haiku-latest");
    }

    public static async Task<GenerateOutput<string>> RunGenerateAsync()
    {
        using GenerateClaude generateAi = Create();
        return await generateAi.GenerateAsync(TestGenerateAiSamples.Prompt, TestGenerateAiSamples.CreateInputs());
    }

    public static async Task<GenerateOutput<string>> RunGenerateWithLimitAsync()
    {
        using GenerateClaude generateAi = Create();
        return await generateAi.GenerateAsync(TestGenerateAiSamples.Prompt, TestGenerateAiSamples.CreateInputs(), limit: 5);
    }

    public static async Task<NotImplementedException> RunGenerateUseToolThrowsAsync()
    {
        return await TestGenerateAiSamples.RunWithTemporaryEnvironmentVariableAsync(
            "ANTHROPIC_API_KEY",
            "test-anthropic-api-key",
            async () =>
        {
            using GenerateClaude generateAi = Create();

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
        using GenerateClaude generateAi = Create();
        return await generateAi.GenerateJsonStrAsync(
            TestGenerateAiSamples.JsonPrompt,
            TestGenerateAiSamples.CreateInputs(),
            TestGenerateAiSamples.CreateJsonSchema());
    }

    public static async Task<GenerateOutput<TestGenerateAiSamples.JsonTestDto>> RunGenerateJsonAsync()
    {
        using GenerateClaude generateAi = Create();
        return await generateAi.GenerateJsonAsync<TestGenerateAiSamples.JsonTestDto>(
            TestGenerateAiSamples.JsonPrompt,
            TestGenerateAiSamples.CreateInputs(),
            TestGenerateAiSamples.CreateJsonSchema());
    }

    public static async Task<List<string>> RunGenerateStreamAsync()
    {
        using GenerateClaude generateAi = Create();
        List<string> chunks = [];

        await foreach (string chunk in generateAi.GenerateStreamAsync(
                           TestGenerateAiSamples.Prompt,
                           TestGenerateAiSamples.CreateInputs()))
        {
            chunks.Add(chunk);
        }

        return chunks;
    }
}
