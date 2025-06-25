namespace AgentsSdk.Models.Agents.Models.OpenAI
{
    public class OpenAIAgentModel : AgentModel
    {
        public override string Provider => "openai";

        public OpenAIModelOptions? Options { get; set; } = default!;
    }
}
