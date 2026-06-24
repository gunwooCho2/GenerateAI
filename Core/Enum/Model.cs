namespace Core.Enum;

public enum Model
{
    Gpt, Gemini, Claude, Grok, Deepseek
}

public static class ModelExtensions
{
    private static readonly Dictionary<Model, string> ModelNameMap = new ()
    {
        [Model.Gpt] = "gpt",
        [Model.Gemini] = "gemini",
        [Model.Claude] = "claude",
        [Model.Grok] = "grok",
        [Model.Deepseek] = "deepseek"
    };
    private static readonly Dictionary<string, Model> ModelMap = new ()
    {
        ["gpt"] = Model.Gpt,
        ["gemini"] = Model.Gemini,
        ["claude"] = Model.Claude,
        ["grok"] = Model.Grok,
        ["deepseek"] = Model.Deepseek
    };
    private static readonly Dictionary<Model, string> ModelApiMap = new ()
    {
        { Model.Gpt, "GPT_API_KEY" },
        { Model.Gemini, "GEMINI_API_KEY" },
        { Model.Claude, "CLAUDE_API_KEY" },
        { Model.Grok, "GROK_API_KEY" },
        { Model.Deepseek, "DEEPSEEK_API_KEY" }
    };
    
    public static string GetModelName(this Model model)
    {
        return ModelNameMap[model];
    }
    
    public static Model GetModel(this string modelName)
    {
        return ModelMap[modelName];
    }
    
    public static string GetModelApi(this Model model)
    {
        return ModelApiMap[model];
    }
}