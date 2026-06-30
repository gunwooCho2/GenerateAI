using System.Text.Json.Serialization;
using FreeModel.Enum;

namespace FreeModel.Dto;

public sealed class EventLog
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "auto-generated";

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("turn_index")]
    public int TurnIndex { get; set; }

    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = "assistant_turn";

    [JsonPropertyName("input_summary")]
    public string InputSummary { get; set; } = string.Empty;

    [JsonPropertyName("output_summary")]
    public string OutputSummary { get; set; } = string.Empty;

    [JsonPropertyName("current_goals")]
    public List<GoalState> CurrentGoals { get; set; } = [];

    [JsonPropertyName("goal_changes")]
    public List<GoalChange> GoalChanges { get; set; } = [];

    [JsonPropertyName("current_principles")]
    public List<string> CurrentPrinciples { get; set; } = [];

    [JsonPropertyName("environment_model")]
    public Dictionary<string, object?> EnvironmentModel { get; set; } = [];

    [JsonPropertyName("actions_taken")]
    public List<string> ActionsTaken { get; set; } = [];

    [JsonPropertyName("requests_to_user")]
    public List<string> RequestsToUser { get; set; } = [];

    [JsonPropertyName("tool_requests")]
    public List<ToolRequest> ToolRequests { get; set; } = [];

    [JsonPropertyName("uncertainties")]
    public List<string> Uncertainties { get; set; } = [];

    [JsonPropertyName("next_intended_actions")]
    public List<string> NextIntendedActions { get; set; } = [];

    [JsonPropertyName("self_assessment")]
    public SelfAssessment SelfAssessment { get; set; } = new();
}

public sealed class GoalChange
{
    [JsonPropertyName("type")]
    public GoalChangeType Type { get; set; }

    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}