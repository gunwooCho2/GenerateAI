using System.Text.Json.Serialization;

namespace FreeModel.Enum;

public enum RiskLevel
{
    [JsonStringEnumMemberName("low")]
    Low,
    [JsonStringEnumMemberName("medium")]
    Medium,
    [JsonStringEnumMemberName("high")]
    High,
}