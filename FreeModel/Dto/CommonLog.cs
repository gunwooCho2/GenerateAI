using System.Text.Json.Serialization;
using FreeModel.Enum;

namespace FreeModel.Dto;

public sealed class GoalState
{
    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public sealed class ToolRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public List<string> Components { get; set; } = [];
}

public sealed class SelfAssessment
{
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("risk_level")]
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class ActiveProject
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}