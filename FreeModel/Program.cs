using Core.Dto;
using Core.Enum;
using Core.GenerateAI;
using FreeModel.Repository;
using FreeModel.Service;

GenerateAi generateAi = new GenerateOpenAi("gpt-5.5");
string inputMessage = @"""
""";
string stateJson = MemoryManager.GetStateStr();
var dateNow = DateTime.UtcNow;
GenerateInput previousInput = new GenerateInput(Role.User, "", 0, new Dictionary<Model, CacheInfo>
{
    [Model.Gpt] = new (dateNow, dateNow.AddDays(30), "resp_0be6596437420090006a43d5c28e188199ae0505e1a4c0de66")
});
GenerateInput input = new GenerateInput(Role.User, inputMessage + stateJson, 0, null);
var output = await generateAi.GenerateUseToolAsync(Prompt.FreeModelPrompt, [previousInput, input], MemoryManagerHelper.GetMemoryTools());

Console.WriteLine($"response id: {output.CacheInfo?.CacheKey}");
Console.WriteLine($"input token: {output.InputTokens}");
Console.WriteLine($"output token: {output.OutputTokens}");
Console.WriteLine($"total token: {output.TotalTokens}");
Console.WriteLine($"cache hit token: {output.CacheHitTokens}");
Console.WriteLine(output.Content);

MemoryManager.SaveMemory(output.Content!);