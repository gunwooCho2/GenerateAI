using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Core.JsonSchema;

public abstract class JsonSchemaDto
{
    private static readonly JsonSerializerOptions JsonSerializerSchemaOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    private static readonly JsonSerializerOptions CompactKoreanOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };
    
    public string GetJsonString()
    {
        return JsonSerializer.Serialize(this, GetType(), CompactKoreanOptions);
    }

    public JsonObject GetJsonSchema()
    {
        return JsonSchemaDtoResolver.GetJsonSchema(GetType());
    }

    public string GetJsonSchemaJson()
    {
        return GetJsonSchema().ToJsonString(JsonSerializerSchemaOptions);
    }

    public string GetSchema()
    {
        return GetJsonSchemaJson();
    }

    public static JsonObject GetJsonSchema<TDto>() where TDto : JsonSchemaDto
    {
        return JsonSchemaDtoResolver.GetJsonSchema(typeof(TDto));
    }

    public static string GetJsonSchemaJson<TDto>() where TDto : JsonSchemaDto
    {
        return GetJsonSchema<TDto>().ToJsonString(JsonSerializerSchemaOptions);
    }
}
