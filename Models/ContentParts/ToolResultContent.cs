namespace OrchestrationScenarios.Models.ContentParts;

public class ToolResultContent(string pluginName, string functionName, string callId, string functionResult) : ContentPart
{
    public string PluginName { get; set; } = pluginName;
    public string FunctionName { get; set; } = functionName;
    public string CallId { get; set; } = callId;
    public string FunctionResult { get; set; } = functionResult;
}
