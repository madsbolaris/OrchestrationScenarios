using System.Text.Json;
using System.Text.Json.Nodes;

public static class ToolArgumentNormalizer
{
    public static Dictionary<string, object?> NormalizeArguments(JsonNode? schemaNode, Dictionary<string, object?>? input)
    {
        var inputObj = input is not null
            ? JsonNode.Parse(JsonSerializer.Serialize(input))?.AsObject()
            : new JsonObject();

        var result = new JsonObject();

        if (schemaNode is not JsonObject schema ||
            !schema.TryGetPropertyValue("properties", out var propsNode) ||
            propsNode is not JsonObject properties)
        {
            return input ?? [];
        }

        foreach (var (propName, propSchemaNode) in properties)
        {
            if (propSchemaNode is not JsonObject propSchema)
                continue;

            var isReadonly = IsJsonTrue(propSchema["readonly"]);

            var hasDefault = propSchema.TryGetPropertyValue("default", out var defaultNode);

            if (isReadonly && hasDefault)
            {
                result[propName] = DeepClone(defaultNode!);
            }
            else if (inputObj != null && inputObj.TryGetPropertyValue(propName, out var providedValue))
            {
                result[propName] = DeepClone(providedValue!);
            }
            else if (hasDefault)
            {
                result[propName] = DeepClone(defaultNode!);
            }
        }

        return result.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var value = kvp.Value?.Deserialize<object?>();

                // If it's a string and contains "::", strip it
                if (value is string s && s.Contains("::"))
                {
                    return s.Split(["::"], 2, StringSplitOptions.None)[0];
                }

                return value;
            }
        );
    }

    public static Dictionary<string, object?> GetReadOnlyDefaults(JsonNode? schemaNode)
    {
        var result = new Dictionary<string, object?>();

        if (schemaNode is not JsonObject schema ||
            !schema.TryGetPropertyValue("properties", out var propsNode) ||
            propsNode is not JsonObject properties)
        {
            return result;
        }

        foreach (var (propName, propSchemaNode) in properties)
        {
            if (propSchemaNode is not JsonObject propSchema)
                continue;

            var isReadonly = IsJsonTrue(propSchema["readonly"]);
            var hasDefault = propSchema.TryGetPropertyValue("default", out var defaultNode);

            if (isReadonly && hasDefault)
            {
                result[propName] = defaultNode?.Deserialize<object>();
            }
        }

        return result;
    }

    public static bool IsJsonTrue(JsonNode? node)
    {
        if (node is null) return false;

        return (node is JsonValue v && (
            v.TryGetValue<bool>(out var b) && b ||
            v.TryGetValue<string>(out var s) && bool.TryParse(s, out var parsed) && parsed
        ));
    }

    private static JsonNode? DeepClone(JsonNode? node) =>
        node is null ? null : JsonNode.Parse(node.ToJsonString());
}
