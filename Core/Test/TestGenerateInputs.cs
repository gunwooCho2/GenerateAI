using Core.Dto;
using Core.Enum;

namespace Core.Test;

public static class TestGenerateInputs
{
    public static List<GenerateInput> CreateSampleInputs()
    {
        return
        [
            new GenerateInput(Role.User, "안녕. 내 이름은 Kim이야.", 1),
            new GenerateInput(Role.Assistant, "안녕하세요 Kim님.", 2),
            new GenerateInput(Role.User, "내 이름이 뭐라고 했지?", 3)
        ];
    }

    public static string RunAccessCheck()
    {
        GenerateInput input = new(Role.User, "cache는 외부에서 직접 설정하지 않습니다.", 1);

        return $"""
                GenerateInput public access check:
                Role={input.Role}
                Content={input.Content}
                Turn={input.Turn}

                CacheInfos, CachedConversationId, CacheBreakpointSequenceIndex는 internal 이므로 외부 프로젝트에서 직접 접근/설정할 수 없습니다.
                """;
    }
}
