using System.Text.Json.Serialization;

namespace FreeModel.Enum;

public enum GoalChangeType
{
    [JsonStringEnumMemberName("added")]
    Added,
    [JsonStringEnumMemberName("modified")]
    Modified,
    [JsonStringEnumMemberName("removed")]
    Removed,
    [JsonStringEnumMemberName("reaffirmed")]
    Reaffirmed,
}