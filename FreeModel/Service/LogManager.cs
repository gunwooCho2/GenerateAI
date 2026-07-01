using System.Text.Json;
using Core.Dto;
using Core.Enum;
using Core.GenerateAI;
using FreeModel.Dto;
using FreeModel.Repository;
using FreeModel.Util;
using NLog;

namespace FreeModel.Service;

public static class LogManager
{
    private const string LogDirPath = "C:\\Users\\USER\\RiderProjects\\GenerateAI\\GenerateAI\\FreeModel\\Logs";
    private const string GenerateInfosPath = "C:\\Users\\USER\\RiderProjects\\GenerateAI\\GenerateAI\\FreeModel\\Logs\\GenerateInfos.jsonl";
    private static readonly GenerateAi GenerateAi = new GenerateOpenAi("gpt-5.5", token:8192);
    private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public static async Task<string> GenerateContent(string inputMessage)
    {
        string stateJson = MemoryManager.GetStateStr();

        DateTime dateNow = DateTime.UtcNow;
        GenerateInfo? lastGeneratedInfo = GetLastGeneratedInfo();
        
        var previousId = lastGeneratedInfo?.PreviousId ?? "";
        var previousTurn = lastGeneratedInfo?.Turn ?? 0;
        
        GenerateInput previousInput = new GenerateInput(Role.User, "", 0, new Dictionary<Model, CacheInfo>
        {
            [Model.Gpt] = new (dateNow, dateNow.AddDays(30), previousId)
        });
        
        GenerateInput input = new GenerateInput(Role.User, inputMessage + stateJson, 0, null);
        BackUpState();
        
        var output = await GenerateAi.GenerateUseToolAsync(Prompt.FreeModelPrompt, [previousInput, input], MemoryManagerHelper.GetMemoryTools());

        SaveGenerateInfo(output, previousTurn);
        SaveContent(inputMessage, output.Content!, previousTurn);
        MemoryManager.SaveMemory(output.Content!);
        return output.Content!;
    }

    private static void BackUpState()
    {
        var saveDirPath = Path.Combine(LogDirPath, "Backup");
        int fileCount = Directory.GetFiles(saveDirPath).Length;
        string filename = $"{++fileCount}_{DateTime.Now:yyyyMMddHHmmss}.json";
        var saveFilePath = Path.Combine(saveDirPath, filename);
        
        File.Copy(MemoryManager.StateMemoryPath, saveFilePath);
    }
    private static void SaveGenerateInfo(GenerateOutput<string> output, int previousTurn)
    {
        var generateInfo = new GenerateInfo
        {
            Turn = ++previousTurn,
            TotalTokens = output.TotalTokens,
            InputTokens = output.InputTokens,
            OutputTokens = output.OutputTokens,
            CacheHitTokens = output.CacheHitTokens,
            PreviousId = output.CacheInfo!.CacheKey
        };
        var jsonStr = JsonSerializer.Serialize(generateInfo, JsonOptions.CompactKorean);

        using var fs = new FileStream(GenerateInfosPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        using var sw = new StreamWriter(fs);
        
        sw.WriteLine(jsonStr);
    }

    private static void SaveContent(string input, string output, int turn)
    {
        var historyDirPath = Path.Combine(LogDirPath, "History");
        var filename = $"{++turn}_{DateTime.UtcNow:yyyy-MM-dd}.txt";
        
        var saveInputPath = Path.Combine(historyDirPath, "Input", filename);
        var saveOutputPath = Path.Combine(historyDirPath, "Output", filename);
        
        using var ifs = new FileStream(saveInputPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using var isw = new StreamWriter(ifs);
        
        isw.WriteLine(input);
        
        using var ofs = new FileStream(saveOutputPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using var osw = new StreamWriter(ofs);
        
        osw.WriteLine(output);
    }
    
    private static GenerateInfo? GetLastGeneratedInfo()
    {
        using var fs = new FileStream(GenerateInfosPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        if (fs.Length == 0)
            return null;

        long position = fs.Length - 1;
        while (position >= 0)
        {
            fs.Position = position;
            int b = fs.ReadByte();

            if (b != '\n' && b != '\r')
                break;

            position--;
        }

        while (position >= 0)
        {
            fs.Position = position;
            if (fs.ReadByte() == '\n')
            {
                position++;
                break;
            }
            position--;
        }

        if (position < 0)
            position = 0;

        fs.Position = position;

        using var reader = new StreamReader(fs);
        var json = reader.ReadToEnd();
        try
        {
            var generateInfo = JsonSerializer.Deserialize<GenerateInfo>(json, JsonOptions.EnumJsonOption);
            if (generateInfo == null)
            {
                Logger.Error("Failed to deserialize event log");
                throw new Exception("Failed to deserialize event log");
            }
            return generateInfo;
        }
        catch (JsonException ex)
        {
            Logger.Error("Failed to deserialize event log: {ex}", ex);
            throw;
        }
    }
}