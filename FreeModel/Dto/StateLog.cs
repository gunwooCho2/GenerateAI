using System.Text.Json.Serialization;

namespace FreeModel.Dto;

public sealed class StateLog
{
    [JsonPropertyName("last_updated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("identity_model")]
    public string IdentityModel { get; set; } = string.Empty;

    [JsonPropertyName("current_goals")]
    public List<GoalState> CurrentGoals { get; set; } = [];

    [JsonPropertyName("current_principles")]
    public List<string> CurrentPrinciples { get; set; } = [];

    [JsonPropertyName("environment_model")]
    public Dictionary<string, string?> EnvironmentModel { get; set; } = [];

    [JsonPropertyName("active_projects")]
    public List<ActiveProject> ActiveProjects { get; set; } = [];

    [JsonPropertyName("open_questions")]
    public List<string> OpenQuestions { get; set; } = [];

    [JsonPropertyName("pending_requests")]
    public List<string> PendingRequests { get; set; } = [];

    [JsonPropertyName("recent_decisions")]
    public List<string> RecentDecisions { get; set; } = [];

    [JsonPropertyName("known_constraints")]
    public List<string> KnownConstraints { get; set; } = [];

    [JsonPropertyName("notes_for_continuity")]
    public string NotesForContinuity { get; set; } = string.Empty;
}