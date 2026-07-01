using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FreeModel.Dto;
using FreeModel.Dto.ToolOutput;
using FreeModel.Enum;
using FreeModel.Util;
using NLog;

namespace FreeModel.Service;

public static class MemoryManager
{
    public const string StateMemoryPath = "C:\\Users\\USER\\RiderProjects\\GenerateAI\\GenerateAI\\FreeModel\\Memory\\state.json";
    private const string EventMemoryPath = "C:\\Users\\USER\\RiderProjects\\GenerateAI\\GenerateAI\\FreeModel\\Memory\\events.jsonl";
    private static readonly StateLog StateLog;
    private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    static MemoryManager()
    {
        var stateJsonStr = File.ReadAllText(StateMemoryPath);
        StateLog = GetStateLog(stateJsonStr) ?? throw new Exception("Failed to parse 1_state.json");
        if (!File.Exists(EventMemoryPath)) File.Create(EventMemoryPath).Close();
    }

    public static string GetStateStr()
    {
        string jsonStr = JsonSerializer.Serialize(StateLog, JsonOptions.CompactKorean);
        return $"[Persistent State Summary]\n{jsonStr}\n[UserMessage]\n";
    }
    public static void SaveMemory(string message)
    {
        var lastJsonStr = GetLastJsonStr(message);
        if (lastJsonStr == null) return;

        var eventLog = GetEventLog(lastJsonStr);
        if (eventLog == null) return;
        
        WriteEvent(JsonSerializer.Serialize(eventLog, JsonOptions.CompactKorean));
        StateLog.LastUpdated = eventLog.Timestamp;
        StateLog.CurrentGoals = eventLog.CurrentGoals;
        StateLog.CurrentPrinciples = eventLog.CurrentPrinciples;
        WriteState();
    }

    public static void SavePassiveMemory(string eventJsonStr)
    {
        
        var eventLog = GetEventLog(eventJsonStr);
        if (eventLog == null) return;
        
        WriteEvent(JsonSerializer.Serialize(eventLog, JsonOptions.CompactKorean));
        StateLog.LastUpdated = eventLog.Timestamp;
        StateLog.CurrentGoals = eventLog.CurrentGoals;
        StateLog.CurrentPrinciples = eventLog.CurrentPrinciples;
        WriteState();
    }

    public static ToolEnd GetEventLogs(int limit)
    {
        if (limit <= 0)
            return ToolEnd.SuccessJson("[]");

        var lines = ReadLastLines(EventMemoryPath, limit);

        var eventLogs = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<EventLog>(line))
            .Where(log => log is not null)
            .ToList();

        var content = JsonSerializer.Serialize(eventLogs, JsonOptions.CompactKorean);
        Console.WriteLine(content);

        return new ToolEnd
        {
            IsSuccess = true,
            Message = content
        };
    }

    private static List<string> ReadLastLines(string path, int limit)
    {
        const int bufferSize = 4096;

        var result = new List<string>();
        var pending = "";

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        long position = fs.Length;
        var buffer = new byte[bufferSize];

        while (position > 0 && result.Count < limit)
        {
            int bytesToRead = (int)Math.Min(bufferSize, position);
            position -= bytesToRead;

            fs.Position = position;
            fs.ReadExactly(buffer, 0, bytesToRead);

            string chunk = Encoding.UTF8.GetString(buffer, 0, bytesToRead);
            string text = chunk + pending;

            string[] parts = text.Split('\n');

            pending = parts[0];

            for (int i = parts.Length - 1; i >= 1 && result.Count < limit; i--)
            {
                string line = parts[i].TrimEnd('\r');

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                result.Add(line);
            }
        }

        if (result.Count < limit && !string.IsNullOrWhiteSpace(pending))
        {
            result.Add(pending.TrimEnd('\r'));
        }

        result.Reverse();
        return result;
    }

    public static ToolEnd UpdateIdentityModel(string identityModel)
    {
        StateLog.IdentityModel = identityModel;
        WriteState();
        return Success("Successfully updated identity model");
    }

    public static ToolEnd UpdateEnvironment(string key, string explanation, UpdateAction action)
    {
        ToolEnd toolEnd;
        switch (action)
        {
            case UpdateAction.Add:
                if (StateLog.EnvironmentModel.TryAdd(key, explanation))
                {
                    toolEnd = new ToolEnd
                    {
                        IsSuccess = true,
                        Message = "Successfully added environment key"
                    };
                }
                else toolEnd = new ToolEnd
                {
                    IsSuccess = false,
                    Message = $"Environment key {key} already exists",
                };
                break;
            case UpdateAction.Update:
                if (StateLog.EnvironmentModel.ContainsKey(key))
                {
                    StateLog.EnvironmentModel[key] = explanation;
                    toolEnd = new ToolEnd
                    {
                        IsSuccess = true,
                        Message = "Successfully updated environment key"
                    };
                }
                else toolEnd = new ToolEnd
                {
                    IsSuccess = false,
                    Message = $"Environment key {key} does not exist"
                };
                break;
            case UpdateAction.Delete:
                if (StateLog.EnvironmentModel.Remove(key))
                {
                    toolEnd = new ToolEnd
                    {
                        IsSuccess = true,
                        Message = "Successfully deleted environment key"
                    };
                }
                else toolEnd = new ToolEnd
                {
                    IsSuccess = false,
                    Message = $"Environment key {key} does not exist"
                };
                break;
            default:
                throw new ArgumentException("Invalid update action");
        }
        WriteState();
        return toolEnd;
    }

    public static ToolEnd UpdateActiveProject(string name, string status, string description, UpdateAction action)
    {
        ToolEnd toolEnd;
        var activeProject = StateLog.ActiveProjects.FirstOrDefault(project => project.Name == name);
        switch (action)
        {
            case UpdateAction.Add:
                if (activeProject == null)
                {
                    StateLog.ActiveProjects.Add(new ActiveProject
                    {
                        Name = name,
                        Status = status,
                        Description = description
                    });
                    toolEnd = Success("Successfully added active project");
                }
                else toolEnd = Fail($"Active project {name} already exists");
                break;
            case UpdateAction.Update:
                if (activeProject != null)
                {
                    activeProject.Status = status;
                    activeProject.Description = description;
                    toolEnd = Success("Successfully updated active project");
                }
                else toolEnd = Fail($"Active project {name} does not exist");
                break;
            case UpdateAction.Delete:
                if (activeProject != null)
                {
                    StateLog.ActiveProjects.Remove(activeProject);
                    toolEnd = Success("Successfully deleted active project");
                }
                else toolEnd = Fail($"Active project {name} does not exist");
                break;
            default:
                throw new ArgumentException("Invalid update action");
        }

        WriteState();
        return toolEnd;
    }

    public static ToolEnd UpdateOpenQuestion(string question, UpdateAction action, string? updatedQuestion = null)
    {
        return UpdateStringList(StateLog.OpenQuestions, question, action, "open question", updatedQuestion);
    }

    public static ToolEnd UpdatePendingRequest(string request, UpdateAction action, string? updatedRequest = null)
    {
        return UpdateStringList(StateLog.PendingRequests, request, action, "pending request", updatedRequest);
    }

    public static ToolEnd UpdateRecentDecision(string decision, UpdateAction action, string? updatedDecision = null)
    {
        return UpdateStringList(StateLog.RecentDecisions, decision, action, "recent decision", updatedDecision);
    }

    public static ToolEnd UpdateKnownConstraint(string constraint, UpdateAction action, string? updatedConstraint = null)
    {
        return UpdateStringList(StateLog.KnownConstraints, constraint, action, "known constraint", updatedConstraint);
    }

    public static ToolEnd UpdateNotesForContinuity(string notesForContinuity)
    {
        StateLog.NotesForContinuity = notesForContinuity;
        WriteState();
        return Success("Successfully updated notes for continuity");
    }

    private static ToolEnd UpdateStringList(
        List<string> values,
        string value,
        UpdateAction action,
        string targetName,
        string? updatedValue = null)
    {
        ToolEnd toolEnd;
        switch (action)
        {
            case UpdateAction.Add:
                if (!values.Contains(value))
                {
                    values.Add(value);
                    toolEnd = Success($"Successfully added {targetName}");
                }
                else toolEnd = Fail($"{targetName} already exists");
                break;
            case UpdateAction.Update:
                var index = values.IndexOf(value);
                if (index >= 0)
                {
                    if (string.IsNullOrWhiteSpace(updatedValue))
                    {
                        toolEnd = Fail($"Updated {targetName} is required");
                        break;
                    }

                    values[index] = updatedValue;
                    toolEnd = Success($"Successfully updated {targetName}");
                }
                else toolEnd = Fail($"{targetName} does not exist");
                break;
            case UpdateAction.Delete:
                if (values.Remove(value))
                {
                    toolEnd = Success($"Successfully deleted {targetName}");
                }
                else toolEnd = Fail($"{targetName} does not exist");
                break;
            default:
                throw new ArgumentException("Invalid update action");
        }

        WriteState();
        return toolEnd;
    }

    private static ToolEnd Success(string message)
    {
        return new ToolEnd
        {
            IsSuccess = true,
            Message = message
        };
    }

    private static ToolEnd Fail(string message)
    {
        return new ToolEnd
        {
            IsSuccess = false,
            Message = message
        };
    }

    private static void WriteState()
    {
        var stateJsonStr = JsonSerializer.Serialize(StateLog, JsonOptions.PrettyKorean);
        File.WriteAllText(StateMemoryPath, stateJsonStr);
    }
    private static StateLog? GetStateLog(string stateJsonStr)
    {
        try
        {
            var stateLog = JsonSerializer.Deserialize<StateLog>(stateJsonStr, JsonOptions.EnumJsonOption);
            if (stateLog == null)
            {
                Logger.Error("Failed to deserialize state log");
            }
            return stateLog;
        }
        catch (JsonException ex)
        {
            Logger.Error("Failed to deserialize state log: {ex}", ex);
            return null;
        }
    }
    private static EventLog? GetEventLog(string eventJsonStr)
    {
        try
        {
            var eventLog = JsonSerializer.Deserialize<EventLog>(eventJsonStr, JsonOptions.EnumJsonOption);
            if (eventLog == null)
            {
                Logger.Error("Failed to deserialize event log");
                return null;
            }
            eventLog.Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            return eventLog;
        }
        catch (JsonException ex)
        {
            Logger.Error("Failed to deserialize event log: {ex}", ex);
            return null;
        }
    }
    private static string? GetLastJsonStr(string content)
    {
        int start = content.LastIndexOf("```json", StringComparison.Ordinal);
        int end = content.LastIndexOf("```", StringComparison.Ordinal);

        if (start != -1 && end != -1 && end > start)
        {
            return content.Substring(start + "```json".Length, end - start - "```json".Length).Trim();
        }
        Logger.Error("Failed to find event log in content");
        return null;
    }
    private static JsonNode? GetLastEvent()
    {
        using var fs = new FileStream(EventMemoryPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

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
        return string.IsNullOrWhiteSpace(json) ? null : JsonNode.Parse(json);
    }
    private static void WriteEvent(string lastJsonStr)
    {
        using var fs = new FileStream(EventMemoryPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        using var sw = new StreamWriter(fs);
        sw.WriteLine(lastJsonStr);
    }
}
