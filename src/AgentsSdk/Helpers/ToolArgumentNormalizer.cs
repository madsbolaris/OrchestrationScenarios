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
            kvp => kvp.Value?.Deserialize<object?>()
        );
    }

    private static JsonNode DeepClone(JsonNode node) =>
        JsonNode.Parse(node.ToJsonString())!;

    private static bool IsJsonTrue(JsonNode? node)
    {
        if (node is null) return false;

        return (node is JsonValue v && (
            v.TryGetValue<bool>(out var b) && b ||
            v.TryGetValue<string>(out var s) && bool.TryParse(s, out var parsed) && parsed
        ));
    }
}
