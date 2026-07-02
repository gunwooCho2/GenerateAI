using System.Text.Json.Nodes;

namespace Core.JsonSchema;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public sealed class JsonSchemaFieldAttribute(string description) : Attribute
{
    public string Description { get; } = description;
    public bool Required { get; init; } = true;
    public string? Type { get; init; }
    public string? Format { get; init; }
    public string[]? Enum { get; init; }
    public int MinLength { get; init; } = -1;
    public int MaxLength { get; init; } = -1;
    public double Minimum { get; init; } = double.NaN;
    public double Maximum { get; init; } = double.NaN;
    public bool AllowAdditionalProperties { get; init; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public sealed class JsonSchemaIgnoreAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class JsonSchemaOverrideAttribute(string memberName, string description) : Attribute
{
    public string MemberName { get; } = memberName;
    public string Description { get; } = description;
    public bool Required { get; init; } = true;
    public string? Type { get; init; }
    public string? Format { get; init; }
    public string[]? Enum { get; init; }
    public int MinLength { get; init; } = -1;
    public int MaxLength { get; init; } = -1;
    public double Minimum { get; init; } = double.NaN;
    public double Maximum { get; init; } = double.NaN;
    public bool AllowAdditionalProperties { get; init; }

    internal JsonObject ApplyTo(JsonObject schema)
    {
        JsonSchemaAttributeApplier.Apply(schema, this);
        return schema;
    }
}

internal static class JsonSchemaAttributeApplier
{
    public static void Apply(JsonObject schema, JsonSchemaFieldAttribute attribute)
    {
        ApplyCommon(
            schema,
            attribute.Description,
            attribute.Type,
            attribute.Format,
            attribute.Enum,
            attribute.MinLength,
            attribute.MaxLength,
            attribute.Minimum,
            attribute.Maximum,
            attribute.AllowAdditionalProperties);
    }

    public static void Apply(JsonObject schema, JsonSchemaOverrideAttribute attribute)
    {
        ApplyCommon(
            schema,
            attribute.Description,
            attribute.Type,
            attribute.Format,
            attribute.Enum,
            attribute.MinLength,
            attribute.MaxLength,
            attribute.Minimum,
            attribute.Maximum,
            attribute.AllowAdditionalProperties);
    }

    private static void ApplyCommon(
        JsonObject schema,
        string description,
        string? type,
        string? format,
        string[]? enumValues,
        int minLength,
        int maxLength,
        double minimum,
        double maximum,
        bool allowAdditionalProperties)
    {
        schema["description"] = description;

        if (!string.IsNullOrWhiteSpace(type))
        {
            schema["type"] = type;
        }

        if (!string.IsNullOrWhiteSpace(format))
        {
            schema["format"] = format;
        }

        if (enumValues is { Length: > 0 })
        {
            JsonArray array = new();
            foreach (string value in enumValues)
            {
                array.Add(value);
            }

            schema["enum"] = array;
        }

        if (minLength >= 0)
        {
            schema["minLength"] = minLength;
        }

        if (maxLength >= 0)
        {
            schema["maxLength"] = maxLength;
        }

        if (!double.IsNaN(minimum))
        {
            schema["minimum"] = minimum;
        }

        if (!double.IsNaN(maximum))
        {
            schema["maximum"] = maximum;
        }

        if (allowAdditionalProperties)
        {
            schema["additionalProperties"] = true;
        }
    }
}
