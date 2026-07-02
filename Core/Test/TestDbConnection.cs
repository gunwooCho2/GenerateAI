using Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Core.Test;

public static class TestDbConnection
{
    public static string GetDbConnectionConditionGuide()
    {
        return $"""
                DB 연결 조건:
                1. 환경변수 {GenerateAiDbContextFactory.ConnectionStringEnvironmentName}에 PostgreSQL 연결 문자열이 있어야 합니다.
                2. 연결 문자열 예시:
                   Host=localhost;Port=5432;Database=generate_ai;Username=postgres;Password=postgres
                3. 환경변수가 없거나 빈 문자열이면 Core는 DB 기능을 비활성화하고 API 호출만 수행합니다.
                4. 환경변수가 있으면 GenerateAiDbContextFactory.CreateFromEnvironment()가 Npgsql DbContext를 생성합니다.
                5. PostgreSQL schema 이름은 "{GenerateAiDbContext.DatabaseSchemaName}"입니다.
                6. "{GenerateAiDbContext.DatabaseSchemaName}" schema가 없으면 Core가 자동 생성합니다.
                7. 필요한 테이블이 없으면 Core가 CREATE TABLE IF NOT EXISTS로 자동 생성합니다.
                8. 필요한 인덱스도 CREATE INDEX IF NOT EXISTS로 자동 생성합니다.
                9. PostgreSQL database 자체는 미리 존재해야 합니다. Core가 database를 생성하지는 않습니다.
                10. ModelEntity 초기 데이터는 Model enum 값 기준으로 seed 됩니다.
                """;
    }

    public static async Task<string> CheckCanConnectAsync()
    {
        await using GenerateAiDbContext? dbContext =
            GenerateAiDbContextFactory.CreateFromEnvironment();

        if (dbContext == null)
        {
            return $"DB disabled: {GenerateAiDbContextFactory.ConnectionStringEnvironmentName} is not set.";
        }

        bool canConnect = await dbContext.Database.CanConnectAsync();

        return canConnect
            ? "DB connected."
            : "DB connection failed.";
    }

    public static async Task<string> EnsureCreatedAndCheckModelsAsync()
    {
        await using GenerateAiDbContext? dbContext =
            GenerateAiDbContextFactory.CreateFromEnvironment();

        if (dbContext == null)
        {
            return $"DB disabled: {GenerateAiDbContextFactory.ConnectionStringEnvironmentName} is not set.";
        }

        int modelCount = await dbContext.Models.CountAsync();

        return $"DB ready. Schema={GenerateAiDbContext.DatabaseSchemaName}, seed model count={modelCount}.";
    }

    public static void PrintGuide()
    {
        Console.WriteLine(GetDbConnectionConditionGuide());
    }
}
