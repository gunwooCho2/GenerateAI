namespace Core.GenerateAI;

public class GenerateOpenAi(string modelName, int token = 4096): GenerateOpenAiBase(modelName, GetApiKey("OPENAI_API_KEY"), null, token)
{
    
}