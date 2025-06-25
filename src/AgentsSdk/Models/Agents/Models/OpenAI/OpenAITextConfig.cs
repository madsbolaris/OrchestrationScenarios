namespace AgentsSdk.Models.Agents.Models.OpenAI;

public class OpenAITextConfig
{
    public OpenAITextConfigFormat Format { get; set; }
}

public enum OpenAITextConfigFormat
{
    Text,
    JsonObject,
    JsonSchema
}
