namespace AgentsSdk.Models.Runs.Requests.Options;

/// <summary>
/// Defines how messages are truncated if they exceed model input limits.
/// </summary>
public class TruncationStrategy
{
    /// <summary>
    /// The type of truncation strategy to apply.
    /// </summary>
    public string Type { get; set; } = default!; // "auto" | "last_messages"

    /// <summary>
    /// The number of most recent messages to retain when using 'last_messages' strategy.
    /// </summary>
    public int? LastMessages { get; set; }
}
