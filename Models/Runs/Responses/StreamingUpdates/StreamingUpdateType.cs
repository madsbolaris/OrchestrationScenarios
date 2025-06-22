// <copyright file="StreamingUpdateType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

/// <summary>
/// Represents the type of a streaming run update.
/// </summary>
[JsonConverter(typeof(StreamingUpdateTypeConverter))]
public readonly partial struct StreamingUpdateType : IEquatable<StreamingUpdateType>
{
    private readonly string _value;

    public StreamingUpdateType(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    // Predefined types
    private const string ThreadValue = "thread";
    private const string RunValue = "run";
    private const string ChatMessageValue = "chat_message";
    private const string AIContentValue = "ai_content";

    public static StreamingUpdateType Thread { get; } = new(ThreadValue);
    public static StreamingUpdateType Run { get; } = new(RunValue);
    public static StreamingUpdateType ChatMessage { get; } = new(ChatMessageValue);
    public static StreamingUpdateType AIContent { get; } = new(AIContentValue);

    public static implicit operator StreamingUpdateType(string value) => new(value);

    public static bool operator ==(StreamingUpdateType left, StreamingUpdateType right) => left.Equals(right);
    public static bool operator !=(StreamingUpdateType left, StreamingUpdateType right) => !left.Equals(right);

    public bool Equals(StreamingUpdateType other) =>
        string.Equals(_value, other._value, StringComparison.InvariantCultureIgnoreCase);

    public override bool Equals(object? obj) => obj is StreamingUpdateType other && Equals(other);

    public override int GetHashCode() =>
        _value != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(_value) : 0;

    public override string ToString() => _value ?? string.Empty;
}

public class StreamingUpdateTypeConverter : JsonConverter<StreamingUpdateType>
{
    public override StreamingUpdateType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new JsonException("StreamingUpdateType cannot be null or empty.");
        }

        return new StreamingUpdateType(value);
    }

    public override void Write(Utf8JsonWriter writer, StreamingUpdateType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
