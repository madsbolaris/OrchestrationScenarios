namespace OrchestrationScenarios.Models.Runs.Requests.Options;

/// <summary>
/// Configuration options for generating completions.
/// </summary>
public class RunOptions
{
    /// <summary>
    /// Strategy for truncating messages when input exceeds model limits.
    /// </summary>
    public TruncationStrategy? TruncationStrategy { get; set; }
}
