using System.Collections;
using System.Text.Json.Nodes;

namespace Core.JsonSchema;

internal static class JsonSchemaTypeMapper
{
    public static JsonObject CreateSchema(Type type)
    {
        Type actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (actualType == typeof(string) || actualType == typeof(char))
        {
            return new JsonObject { ["type"] = "string" };
        }

        if (actualType == typeof(bool))
        {
            return new JsonObject { ["type"] = "boolean" };
        }

        if (IsInteger(actualType))
        {
            return new JsonObject { ["type"] = "integer" };
        }

        if (IsNumber(actualType))
        {
            return new JsonObject { ["type"] = "number" };
        }

        if (actualType == typeof(DateTime) || actualType == typeof(DateTimeOffset))
        {
            return new JsonObject { ["type"] = "string", ["format"] = "date-time" };
        }

        if (actualType == typeof(DateOnly))
        {
            return new JsonObject { ["type"] = "string", ["format"] = "date" };
        }

        if (actualType == typeof(TimeOnly))
        {
            return new JsonObject { ["type"] = "string", ["format"] = "time" };
        }

        if (actualType == typeof(Guid))
        {
            return new JsonObject { ["type"] = "string", ["format"] = "uuid" };
        }

        if (actualType.IsEnum)
        {
            JsonArray values = new();
            foreach (string name in System.Enum.GetNames(actualType))
            {
                values.Add(name);
            }

            return new JsonObject { ["type"] = "string", ["enum"] = values };
        }

        if (TryGetDictionaryValueType(actualType, out Type? valueType))
        {
            return new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = CreateSchema(valueType!)
            };
        }

        if (TryGetEnumerableElementType(actualType, out Type? elementType))
        {
            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = CreateSchema(elementType!)
            };
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false
        };
    }

    private static bool IsInteger(Type type)
    {
        return type == typeof(byte)
               || type == typeof(sbyte)
               || type == typeof(short)
               || type == typeof(ushort)
               || type == typeof(int)
               || type == typeof(uint)
               || type == typeof(long)
               || type == typeof(ulong);
    }

    private static bool IsNumber(Type type)
    {
        return type == typeof(float)
               || type == typeof(double)
               || type == typeof(decimal);
    }

    private static bool TryGetDictionaryValueType(Type type, out Type? valueType)
    {
        valueType = null;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            Type[] arguments = type.GetGenericArguments();
            if (arguments[0] == typeof(string))
            {
                valueType = arguments[1];
                return true;
            }
        }

        Type? dictionaryInterface = type
            .GetInterfaces()
            .FirstOrDefault(item => item.IsGenericType
                                    && item.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                                    && item.GetGenericArguments()[0] == typeof(string));

        if (dictionaryInterface == null)
        {
            return false;
        }

        valueType = dictionaryInterface.GetGenericArguments()[1];
        return true;
    }

    private static bool TryGetEnumerableElementType(Type type, out Type? elementType)
    {
        elementType = null;

        if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return elementType != null;
        }

        Type? enumerableInterface = type
            .GetInterfaces()
            .FirstOrDefault(item => item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface == null)
        {
            return false;
        }

        elementType = enumerableInterface.GetGenericArguments()[0];
        return true;
    }
}
