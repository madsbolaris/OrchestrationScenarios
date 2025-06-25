namespace AgentsSdk.Models;

public class OpenAIConfiguration
{
    public string ModelId { get; set; } = "gpt-4";

    public OpenAISettings OpenAI { get; set; } = new();

    public class OpenAISettings
    {
        public string ApiKey { get; set; } = "";
    }
}
