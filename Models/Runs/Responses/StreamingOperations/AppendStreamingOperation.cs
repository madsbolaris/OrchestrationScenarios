using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;

public class AppendStreamingOperation<T> : StreamingOperation<T>
{
    private readonly string _jsonPath;
    private readonly string _value;

    [JsonPropertyName("p")]
    public override string JsonPath => _jsonPath;

    [JsonPropertyName("v")]
    public object? Value => _value;

    public AppendStreamingOperation(T value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        string? path = null;
        object? propValue = null;

        foreach (var prop in properties)
        {
            if (prop.Name == "Type")
            {
                continue;
            }

            var val = prop.GetValue(value);
            if (IsDefaultValue(val))
            {
                continue;
            }

            if (path != null)
            {
                throw new InvalidOperationException($"AIContent must have exactly one property set. Found both '{path}' and '{prop.Name}'.");
            }

            path = prop.Name;
            propValue = val;
        }

        if (path == null || propValue == null)
        {
            throw new InvalidOperationException("AIContent must have exactly one non-default property set.");
        }

        _jsonPath = path;
        _value = propValue is string str
            ? str
            : JsonSerializer.Serialize(propValue)
                ?? throw new InvalidOperationException($"Property '{path}' cannot be converted to a string.");
    }

    private static bool IsDefaultValue(object? value)
    {
        if (value == null)
        {
            return true;
        }

        var type = value.GetType();
        return type.IsValueType && value.Equals(Activator.CreateInstance(type));
    }
}
