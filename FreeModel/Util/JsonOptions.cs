using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FreeModel.Util;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions PrettyKorean = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static readonly JsonSerializerOptions CompactKorean = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    
    public static readonly JsonSerializerOptions EnumJsonOption = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}