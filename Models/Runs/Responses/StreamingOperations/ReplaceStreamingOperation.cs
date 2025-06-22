// <copyright file="ReplaceStreamingOperation.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;

public class ReplaceStreamingOperation<T> : StreamingOperation<T>
{
    private readonly string _jsonPath;
    private readonly string _value;

    [JsonPropertyName("p")]
    public override string JsonPath => _jsonPath;

    [JsonPropertyName("v")]
    public object? Value => _value;

    [JsonPropertyName("s")]
    public int Start { get; }

    [JsonPropertyName("e")]
    public int End { get; }

    public ReplaceStreamingOperation(int start, int end, T value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Start = start;
        End = end;

        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        string? path = null;
        object? propValue = null;

        foreach (var prop in properties)
        {
            var val = prop.GetValue(value);
            if (IsDefaultValue(val))
            {
                continue;
            }

            if (path != null)
            {
                throw new InvalidOperationException($"AIContent must have exactly one property set. More than one was found: '{path}' and '{prop.Name}'.");
            }

            path = prop.Name;
            propValue = val;
        }

        if (path == null || propValue == null)
        {
            throw new InvalidOperationException("AIContent must have exactly one non-default property set.");
        }

        _jsonPath = path;
        _value = JsonSerializer.Serialize(propValue)
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
