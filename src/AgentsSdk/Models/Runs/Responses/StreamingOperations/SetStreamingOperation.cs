using System.Reflection;
using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Runs.Responses.StreamingOperations;

/// <summary>
/// Represents an operation that sets (overwrites) the value at a JSON path.
/// </summary>
public class SetStreamingOperation<T> : StreamingOperation<T> where T : new()
{
    private readonly string? _jsonPath;
    private readonly object _value;

    [JsonPropertyName("p")]
    public override string? JsonPath => _jsonPath;

    [JsonPropertyName("v")]
    public object? Value => _value;

    [JsonIgnore]
    public T TypedValue
    {
        get
        {
            if (_jsonPath is null)
            {
                // Full object
                return (T)_value;
            }

            // Reconstruct T from a single property
            var instance = new T();
            var prop = typeof(T).GetProperty(_jsonPath, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null || !prop.CanWrite)
            {
                throw new InvalidOperationException($"Property '{_jsonPath}' not found or not writable on type '{typeof(T)}'.");
            }

            var converted = _value == null ? null : Convert.ChangeType(_value, prop.PropertyType);
            prop.SetValue(instance, converted);
            return instance;
        }
    }

    public SetStreamingOperation(T value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var nonDefaults = GetNonDefaultProperties(value);

        if (nonDefaults.Count == 0)
        {
            throw new InvalidOperationException("AIContent must have at least one non-default property set.");
        }

        if (nonDefaults.Count == 1)
        {
            var kvp = nonDefaults.First();
            _jsonPath = kvp.Key;
            _value = kvp.Value ?? throw new InvalidOperationException("Single property value cannot be null.");
        }
        else
        {
            _jsonPath = null;
            _value = value;
        }
    }

    private static Dictionary<string, object?> GetNonDefaultProperties(T value)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var result = new Dictionary<string, object?>();

        foreach (var prop in properties)
        {
            var val = prop.GetValue(value);
            if (IsDefaultValue(val))
            {
                continue;
            }

            result[prop.Name] = val;
        }

        return result;
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
