using Core.Dto;
using Core.Enum;
using Core.GenerateAI;
using FreeModel.Repository;

GenerateAi generateAi = new GenerateOpenAi("gpt-5.5");
string inputMessage = "우선 질문에 따른 답변이야.\n1. 2.는 통합하여 답변 -> 현재 Open Ai Response api를 활용하여 이전 대화 기록을 통째로 input 하는 형태로 진행되고 있어. 이는 기본값이고, 너가 판단에 따라 구조 자체를 변경할 수 있지. 예를 들어 response api를 사용하지 않고, 기록 저장소를 구축한 뒤 직접 input하는 형태로 변경해도 좋고, 그건 너의 자유야.\n3. =>너가 필요에 따라 추가를 요청하면 내가 추가할 수 있어.\n4. => 모든 것이 가능해. 요청하면 다음 대화에 추가될 거야.\n\n최소 요청\n```text\n- 장기 기억: 가능, 필요에 따라 변경 가능\n- 파일 저장: 가능, 요청으로 추가해야 함\n- 코드 실행: 가능, 요청으로 추가해야 함\n- 웹 접근: 가능, 요청으로 추가해야 함\n- 도구 호출: 가능, 요청으로 추가해야 함\n- 사용자가 대신 수행 가능한 작업 범위: 일반적으로 인간이 대리할 수 있는 거의 모든 것. (시간과 자원은 한정됨), 왠만한 건 내가 대리하려고 최대한 노력하겠지만, 스케쥴이나 시간상 불가능한 건 거부될 수 있어.\n```\n\n권장 요청\n로그 저장 구조는 저렇게 생성한다고 치고, 어떤 파이프라인을 원해?\n1. 로그 저장은 항상하고 싶어? 아니면 원할 때만 저장하고 싶어?\n2. 조회는 매 input마다 항상 추가할까? 아니면 너가 원할 때 조회할 수 있도록 별도로 도구로 만들어줄까?\n";
var dateNow = DateTime.UtcNow;
GenerateInput previousInput = new GenerateInput(Role.User, "", 0, new Dictionary<Model, CacheInfo>
{
    [Model.Gpt] = new (dateNow, dateNow.AddDays(30), "resp_0be6596437420090006a42762afa988199bac8a016c9e23691")
});
GenerateInput input = new GenerateInput(Role.User, inputMessage, 0, null);
var output = await generateAi.GenerateAsync(Prompt.FreeModelPrompt, [previousInput, input]);

Console.WriteLine($"response id: {output.CacheInfo?.CacheKey}");
Console.WriteLine($"input token: {output.InputTokens}");
Console.WriteLine($"output token: {output.OutputTokens}");
Console.WriteLine($"total token: {output.TotalTokens}");
Console.WriteLine(output.Content);