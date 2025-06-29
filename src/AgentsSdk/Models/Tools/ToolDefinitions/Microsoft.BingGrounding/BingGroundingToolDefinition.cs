namespace AgentsSdk.Models.Tools.ToolDefinitions.BingGrounding;

public class BingGroundingToolDefinition : ToolDefinition
{
    public override string Type => "Microsoft.BingGrounding";
    public string ConnectionName { get; set; } = null!;

    internal ToolMetadata ToToolMetadata()
    {
        return new ToolMetadata
        {
            Name = "Microsoft.BingGrounding.Search",
            Type = Type,
            Description = "Search the web using Bing",
            Parameters = null,
            Executor = null // handled by OpenAI/Response layer as a native tool
        };
    }
}
