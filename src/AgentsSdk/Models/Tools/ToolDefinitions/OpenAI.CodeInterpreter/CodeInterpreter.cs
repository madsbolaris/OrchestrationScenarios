namespace AgentsSdk.Models.Tools.ToolDefinitions.OpenAI.CodeInterpreter;

public class CodeInterpreterToolDefinition : AgentToolDefinition
{
    public override string Type => "OpenAI.CodeInterpreter";

    public List<string>? FileIds { get; set; }
}
