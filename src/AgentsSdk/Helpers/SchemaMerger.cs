using System.Text.Json.Nodes;

namespace AgentsSdk.Helpers;

public static class SchemaMerger
{
    public static JsonNode? Merge(JsonNode? baseSchema, JsonNode? overrideSchema)
    {
        if (baseSchema is not JsonObject baseObj) return overrideSchema;
        if (overrideSchema is not JsonObject overrideObj) return baseObj;

        var merged = JsonNode.Parse(baseObj.ToJsonString())!.AsObject();

        // Track which properties should be removed from 'required'
        var requiredToRemove = new HashSet<string>();

        // Merge "properties" deeply
        if (overrideObj.TryGetPropertyValue("properties", out var overridePropsNode) &&
            overridePropsNode is JsonObject overrideProps &&
            merged.TryGetPropertyValue("properties", out var mergedPropsNode) &&
            mergedPropsNode is JsonObject mergedProps)
        {
            foreach (var (propName, overridePropSchema) in overrideProps)
            {
                if (overridePropSchema is not JsonObject overridePropObj)
                {
                    mergedProps[propName] = overridePropSchema;
                    continue;
                }

                if (mergedProps.TryGetPropertyValue(propName, out var basePropSchema) &&
                    basePropSchema is JsonObject basePropObj)
                {
                    var mergedProp = JsonNode.Parse(basePropObj.ToJsonString())!.AsObject();

                    foreach (var (fieldKey, fieldVal) in overridePropObj)
                    {
                        mergedProp[fieldKey] = JsonNode.Parse(fieldVal!.ToJsonString());
                    }

                    mergedProps[propName] = mergedProp;
                }
                else
                {
                    mergedProps[propName] = JsonNode.Parse(overridePropObj.ToJsonString());
                }

                // If override defines a "default", this field should no longer be required
                if (overridePropObj.TryGetPropertyValue("default", out _))
                {
                    requiredToRemove.Add(propName);
                }
            }
        }

        // Merge other top-level fields like "title", "description", etc.
        foreach (var (key, val) in overrideObj)
        {
            if (key == "properties") continue;
            merged[key] = JsonNode.Parse(val!.ToJsonString());
        }

        // Remove fields with defaults from "required"
        if (merged.TryGetPropertyValue("required", out var requiredNode) &&
            requiredNode is JsonArray requiredArray)
        {
            var updatedRequired = new JsonArray();

            foreach (var item in requiredArray)
            {
                if (item is JsonValue val &&
                    val.TryGetValue<string>(out var str) &&
                    !requiredToRemove.Contains(str))
                {
                    updatedRequired.Add(str);
                }
            }

            merged["required"] = updatedRequired;
        }

        return merged;
    }
}
