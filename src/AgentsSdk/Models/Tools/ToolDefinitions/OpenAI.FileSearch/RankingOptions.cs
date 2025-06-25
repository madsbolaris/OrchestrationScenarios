namespace AgentsSdk.Models.Tools.ToolDefinitions.OpenAI.FileSearch;

public class RankingOptions
{
    public float ScoreThreshold { get; set; }

    public string? Ranker { get; set; }
}
