using Core.Dto;
using Core.Enum;
using Core.GenerateAI;
using FreeModel.Repository;

GenerateAi generateAi = new GenerateOpenAi("gpt-5.5");
string inputMessage = "내가 무얼하고 있지?";
var dateNow = DateTime.UtcNow;
GenerateInput previousInput = new GenerateInput(Role.User, "", 0, new Dictionary<Model, CacheInfo>
{
    [Model.Gpt] = new (dateNow, dateNow.AddDays(30), "resp_0066fcf2eb72db72006a42337ec44c8199aa39c191a402bcf7")
});
GenerateInput input = new GenerateInput(Role.User, inputMessage, 0, null);
var output = await generateAi.GenerateAsync("대화 시작 항상 'ㅁㅇㅁ'를 추가할 것", [previousInput, input]);

Console.WriteLine($"response id: {output.CacheInfo?.CacheKey}");
Console.WriteLine(output.Content);