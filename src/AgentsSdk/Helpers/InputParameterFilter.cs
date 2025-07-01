using System.Text.Json.Nodes;

public static class InputParameterFilter
{
    private static readonly HashSet<string> AllowedKeys = ["type", "description", "readonly", "default"];

    public static JsonObject Filter(JsonObject original)
    {
        var filtered = new JsonObject();
        foreach (var (key, value) in original)
        {
            if (AllowedKeys.Contains(key))
            {
                filtered[key] = value is null ? null : JsonNode.Parse(value.ToJsonString());
            }
        }

        return filtered;
    }
}
