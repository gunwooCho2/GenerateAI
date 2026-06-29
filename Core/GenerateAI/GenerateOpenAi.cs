#pragma warning disable OPENAI001

using Core.Dto;
using Core.Enum;
using OpenAI.Responses;

namespace Core.GenerateAI;

public class GenerateOpenAi(string modelName, int token = 4096): GenerateAi(modelName, GetApiKey("OPENAI_API_KEY"), token)
{
    private readonly ResponsesClient _client = new(apiKey: GetApiKey("OPENAI_API_KEY"));
    public override async Task<GenerateOutput<string>> GenerateAsync(string prompt, List<GenerateInput> inputs)
    {
        CreateResponseOptions options = new CreateResponseOptions
        {
            Model = ModelName,
            Instructions = prompt,
            MaxOutputTokenCount = Token
        };
        var orderedInputs = inputs.OrderBy(x => x.Turn).ToList();
        int cachedIndex = CachedIndex(orderedInputs);
        
        if (cachedIndex != -1)
        {
            var lastCachedInput = orderedInputs[cachedIndex];
            options.PreviousResponseId = lastCachedInput.CacheInfos[Model.Gpt].CacheKey;
        }

        BuildMessages(options, orderedInputs, cachedIndex); 
        ResponseResult response = await _client.CreateResponseAsync(options);
        return GetGenerateOutput<string>(response);
    }
    
    private int CachedIndex(List<GenerateInput> orderedInputs)
    {
        int firstInvalidIndex = orderedInputs.FindIndex(x =>
            !x.CacheInfos.TryGetValue(Model.Gpt, out var cacheInfo)
            || !cacheInfo.IsUsable);

        return firstInvalidIndex != -1
            ? firstInvalidIndex - 1
            : orderedInputs.Count - 1;
    }
    
    private static void BuildMessages(CreateResponseOptions options, List<GenerateInput> orderedInputs, int cachedIndex)
    {
        for (int i = cachedIndex + 1; i < orderedInputs.Count; i++)
        {
            var input = orderedInputs[i];
            var requestMessage = input.Role == Role.User ? ResponseItem.CreateUserMessageItem(input.Content) : ResponseItem.CreateAssistantMessageItem(input.Content);
            options.InputItems.Add(requestMessage);
        }
    }
    
    private static GenerateOutput<T> GetGenerateOutput<T>(ResponseResult response)
    {
        int totalToken = response.Usage.TotalTokenCount;
        int inputToken = response.Usage.InputTokenCount;
        int outputToken = response.Usage.OutputTokenCount;
        int cacheHitToken = response.Usage.InputTokenDetails.CachedTokenCount;

        var responseId = response.Id;
        var dateNow = DateTime.UtcNow;
        var cacheInfo = new CacheInfo(dateNow, dateNow.AddDays(30), responseId);

        T output;

        if (typeof(T) == typeof(string))
        {
            output = (T)(object)response.GetOutputText();
        }
        else
        {
            throw new NotImplementedException($"Type {typeof(T).Name} is not supported.");
        }

        return new GenerateOutput<T>(
            output,
            totalToken,
            inputToken,
            outputToken,
            cacheHitToken,
            cacheInfo
        );
    }
}