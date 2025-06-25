namespace AgentsSdk.Models.Agents.Models.OpenAI;

public class OpenAIReasoningConfig
{
    public OpenAIReasoningConfigEffort? Effort { get; set; }
    public OpenAIReasoningConfigGenerateSummary? GenerateSummary { get; set; }
}

public enum OpenAIReasoningConfigEffort
{
    Low,
    Medium,
    High
}

public enum OpenAIReasoningConfigGenerateSummary
{
    Concise,
    Detailed
}
