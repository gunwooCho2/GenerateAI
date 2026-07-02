using Core.Dto;
using Core.Enum;
using Core.Interface;
using Core.JsonSchema;

namespace Core.Test;

public static class TestGenerateAiSamples
{
    public const string Prompt = "You are a concise test assistant. Return only the requested answer.";
    public const string JsonPrompt = "Return a JSON object for a successful Generate AI test.";

    public static List<GenerateInput> CreateInputs()
    {
        return
        [
            new GenerateInput(Role.User, "This is Generate AI test input turn 1.", 1),
            new GenerateInput(Role.Assistant, "Acknowledged test input turn 1.", 2),
            new GenerateInput(Role.User, "Reply with a short confirmation for turn 3.", 3)
        ];
    }

    public static List<IToolInfo> CreateNoTools()
    {
        return [];
    }

    public static JsonTestDto CreateJsonSchema()
    {
        return new JsonTestDto();
    }

    public static async Task<T> RunWithTemporaryEnvironmentVariableAsync<T>(
        string name,
        string fallbackValue,
        Func<Task<T>> action)
    {
        string? originalValue = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(originalValue))
        {
            Environment.SetEnvironmentVariable(name, fallbackValue);
        }

        try
        {
            return await action();
        }
        finally
        {
            if (string.IsNullOrWhiteSpace(originalValue))
            {
                Environment.SetEnvironmentVariable(name, null);
            }
        }
    }

    public sealed class JsonTestDto : JsonSchemaDto
    {
        [JsonSchemaField("Short status message for the test result.", MinLength = 1, MaxLength = 80)]
        public string Message { get; init; } = string.Empty;

        [JsonSchemaField("Whether the test request succeeded.")]
        public bool Success { get; init; }
    }
}
