using Core.JsonSchema;

namespace Core.Test;

public static class TestGetJsonSchema
{
    public static string RunUserDtoSchema()
    {
        return JsonSchemaDto.GetJsonSchemaJson<UserDto>();
    }

    public static string RunUserDetailDtoSchema()
    {
        return JsonSchemaDto.GetJsonSchemaJson<UserDetailDto>();
    }

    public static string RunUserDetailDtoJson()
    {
        UserDetailDto userDetailDto = new()
        {
            Name = "Kim",
            Age = 10,
            IncludeInactive = true,
            Sort = UserSearchSort.Recent,
            Email = "kim@example.com",
            Tags = ["admin", "tester"],
            InternalMemo = "schema ignore field"
        };

        return userDetailDto.GetJsonString();
    }

    public static void Print()
    {
        Console.WriteLine(RunUserDtoSchema());
        Console.WriteLine();
        Console.WriteLine(RunUserDetailDtoSchema());
        Console.WriteLine();
        Console.WriteLine(RunUserDetailDtoJson());
    }

    public class UserDto : JsonSchemaDto
    {
        [JsonSchemaField("사용자 이름입니다.", MinLength = 1, MaxLength = 50)]
        public virtual string Name { get; init; } = string.Empty;

        [JsonSchemaField("사용자 나이입니다.", Required = false, Minimum = 0, Maximum = 150)]
        public int Age { get; init; }

        [JsonSchemaField("비활성 사용자 포함 여부입니다.")]
        public bool IncludeInactive { get; init; }

        [JsonSchemaField("검색 결과 정렬 기준입니다.")]
        public UserSearchSort Sort { get; init; }

        [JsonSchemaIgnore]
        public string? InternalMemo { get; init; }
    }

    [JsonSchemaOverride(nameof(Name), "상세 조회 대상 사용자 이름입니다.", MinLength = 2, MaxLength = 80)]
    public sealed class UserDetailDto : UserDto
    {
        [JsonSchemaField("사용자의 이메일 주소입니다.", Format = "email")]
        public string Email { get; init; } = string.Empty;

        [JsonSchemaField("사용자에게 부여된 태그 목록입니다.", Required = false)]
        public IReadOnlyList<string> Tags { get; init; } = [];
    }

    public enum UserSearchSort
    {
        Recent,
        Name,
        Age
    }
}
