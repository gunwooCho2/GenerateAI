using Core.Dto;
using Core.GenerateAI;

namespace Core.Test;

public static class TestGenerateOpenAi
{
    private static GenerateOpenAi Create()
    {
        return new GenerateOpenAi("gpt-4.1-mini");
    }

    public static async Task<GenerateOutput<string>> RunGenerateAsync()
    {
        GenerateOpenAi generateAi = Create();
        return await generateAi.GenerateAsync(TestGenerateAiSamples.Prompt, TestGenerateAiSamples.CreateInputs());
    }

    public static async Task<GenerateOutput<string>> RunGenerateWithLimitAsync()
    {
        GenerateOpenAi generateAi = Create();
        return await generateAi.GenerateAsync(TestGenerateAiSamples.Prompt, TestGenerateAiSamples.CreateInputs(), limit: 5);
    }

    public static async Task<GenerateOutput<string>> RunGenerateUseToolAsync()
    {
        GenerateOpenAi generateAi = Create();
        return await generateAi.GenerateUseToolAsync(
            TestGenerateAiSamples.Prompt,
            TestGenerateAiSamples.CreateInputs(),
            TestGenerateAiSamples.CreateNoTools());
    }

    public static async Task<GenerateOutput<string>> RunGenerateJsonStrAsync()
    {
        GenerateOpenAi generateAi = Create();
        return await generateAi.GenerateJsonStrAsync(
            TestGenerateAiSamples.JsonPrompt,
            TestGenerateAiSamples.CreateInputs(),
            TestGenerateAiSamples.CreateJsonSchema());
    }

    public static async Task<GenerateOutput<TestGenerateAiSamples.JsonTestDto>> RunGenerateJsonAsync()
    {
        GenerateOpenAi generateAi = Create();
        return await generateAi.GenerateJsonAsync<TestGenerateAiSamples.JsonTestDto>(
            TestGenerateAiSamples.JsonPrompt,
            TestGenerateAiSamples.CreateInputs(),
            TestGenerateAiSamples.CreateJsonSchema());
    }
}
