using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Core.JsonSchema;

internal static class JsonSchemaDtoResolver
{
    public static JsonObject GetJsonSchema(Type dtoType)
    {
        if (!typeof(JsonSchemaDto).IsAssignableFrom(dtoType))
        {
            throw new ArgumentException($"{dtoType.Name} must inherit {nameof(JsonSchemaDto)}.");
        }

        Dictionary<string, SchemaMember> members = GetSchemaMembers(dtoType);
        Dictionary<string, JsonSchemaOverrideAttribute> overrides = GetOverrides(dtoType);

        JsonObject properties = new();
        JsonArray required = new();

        foreach (SchemaMember member in members.Values.OrderBy(member => member.Order))
        {
            if (member.Member.GetCustomAttribute<JsonSchemaIgnoreAttribute>(true) != null)
            {
                continue;
            }

            JsonSchemaFieldAttribute? attribute = member.Member.GetCustomAttribute<JsonSchemaFieldAttribute>(true);
            JsonSchemaOverrideAttribute? overrideAttribute = overrides.GetValueOrDefault(member.ClrName);

            if (attribute == null && overrideAttribute == null)
            {
                throw new InvalidOperationException($"{dtoType.Name}.{member.ClrName} does not define JSON schema metadata.");
            }

            JsonObject propertySchema = JsonSchemaTypeMapper.CreateSchema(member.ValueType);
            bool isRequired = true;

            if (attribute != null)
            {
                JsonSchemaAttributeApplier.Apply(propertySchema, attribute);
                isRequired = attribute.Required;
            }

            if (overrideAttribute != null)
            {
                overrideAttribute.ApplyTo(propertySchema);
                isRequired = overrideAttribute.Required;
            }

            properties[member.JsonName] = propertySchema;
            if (isRequired)
            {
                required.Add(member.JsonName);
            }
        }

        ValidateOverrides(dtoType, members, overrides);

        return new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = properties,
            ["required"] = required
        };
    }

    private static Dictionary<string, SchemaMember> GetSchemaMembers(Type dtoType)
    {
        Dictionary<string, SchemaMember> members = new(StringComparer.Ordinal);
        int order = 0;

        foreach (Type type in GetInheritanceChain(dtoType))
        {
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (property.GetIndexParameters().Length > 0 || property.GetMethod == null)
                {
                    continue;
                }

                members[property.Name] = new SchemaMember(
                    property.Name,
                    GetJsonName(property),
                    property.PropertyType,
                    property,
                    order++);
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                members[field.Name] = new SchemaMember(
                    field.Name,
                    GetJsonName(field),
                    field.FieldType,
                    field,
                    order++);
            }
        }

        return members;
    }

    private static Dictionary<string, JsonSchemaOverrideAttribute> GetOverrides(Type dtoType)
    {
        Dictionary<string, JsonSchemaOverrideAttribute> overrides = new(StringComparer.Ordinal);

        foreach (Type type in GetInheritanceChain(dtoType))
        {
            foreach (JsonSchemaOverrideAttribute attribute in type.GetCustomAttributes<JsonSchemaOverrideAttribute>(false))
            {
                overrides[attribute.MemberName] = attribute;
            }
        }

        return overrides;
    }

    private static void ValidateOverrides(
        Type dtoType,
        Dictionary<string, SchemaMember> members,
        Dictionary<string, JsonSchemaOverrideAttribute> overrides)
    {
        foreach (string memberName in overrides.Keys)
        {
            if (!members.ContainsKey(memberName))
            {
                throw new InvalidOperationException($"{dtoType.Name} overrides unknown schema member {memberName}.");
            }
        }
    }

    private static IEnumerable<Type> GetInheritanceChain(Type dtoType)
    {
        Stack<Type> chain = new();
        Type? current = dtoType;

        while (current != null && current != typeof(JsonSchemaDto))
        {
            chain.Push(current);
            current = current.BaseType;
        }

        return chain;
    }

    private static string GetJsonName(MemberInfo member)
    {
        JsonPropertyNameAttribute? attribute = member.GetCustomAttribute<JsonPropertyNameAttribute>();
        return attribute?.Name ?? char.ToLowerInvariant(member.Name[0]) + member.Name[1..];
    }

    private sealed record SchemaMember(
        string ClrName,
        string JsonName,
        Type ValueType,
        MemberInfo Member,
        int Order);
}
