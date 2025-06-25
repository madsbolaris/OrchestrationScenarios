using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentsSdk.Models.Runs.Responses;

/// <summary>
/// Represents the status of a run.
/// </summary>
[JsonConverter(typeof(RunStatusConverter))]
public readonly partial struct RunStatus : IEquatable<RunStatus>
{
    private readonly string _value;

    public RunStatus(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    // Predefined statuses
    private const string InProgressValue = "in_progress";
    private const string IncompleteValue = "incomplete";
    private const string CancelledValue = "cancelled";
    private const string FailedValue = "failed";
    private const string CompletedValue = "completed";
    private const string ExpiredValue = "expired";

    public static RunStatus InProgress { get; } = new(InProgressValue);
    public static RunStatus Incomplete { get; } = new(IncompleteValue);
    public static RunStatus Cancelled { get; } = new(CancelledValue);
    public static RunStatus Failed { get; } = new(FailedValue);
    public static RunStatus Completed { get; } = new(CompletedValue);
    public static RunStatus Expired { get; } = new(ExpiredValue);

    public static implicit operator RunStatus(string value) => new(value);

    public static bool operator ==(RunStatus left, RunStatus right) => left.Equals(right);
    public static bool operator !=(RunStatus left, RunStatus right) => !left.Equals(right);

    public bool Equals(RunStatus other) =>
        string.Equals(_value, other._value, StringComparison.InvariantCultureIgnoreCase);

    public override bool Equals(object? obj) => obj is RunStatus other && Equals(other);

    public override int GetHashCode() =>
        _value != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(_value) : 0;

    public override string ToString() => _value ?? string.Empty;
}

public class RunStatusConverter : JsonConverter<RunStatus>
{
    public override RunStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new JsonException("RunStatus cannot be null or empty.");
        }

        return new RunStatus(value);
    }

    public override void Write(Utf8JsonWriter writer, RunStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
