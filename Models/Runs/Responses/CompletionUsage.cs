namespace OrchestrationScenarios.Models.Runs.Responses;

/// <summary>
/// Tracks token usage statistics for a completion.
/// </summary>
public class CompletionUsage
{
    public long OutputTokens { get; set; }

    public long InputTokens { get; set; }

    public long TotalTokens { get; set; }

    public InputTokenDetails? InputTokenDetails { get; set; }

    public OutputTokenDetails? OutputTokenDetails { get; set; }
}

public class InputTokenDetails
{
    public int? CachedTokens { get; set; }
}

public class OutputTokenDetails
{
    public int? ReasoningTokens { get; set; }
}
