using System.Text.Json.Serialization;

namespace FreeModel.Dto;

public class GenerateInfo
{
    [JsonPropertyName("turn")]
    public int Turn { get; set; }
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }
    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
    [JsonPropertyName("cache_hit_tokens")]
    public int CacheHitTokens { get; set; }
    [JsonPropertyName("previous_id")]
    public string PreviousId { get; set; }
}