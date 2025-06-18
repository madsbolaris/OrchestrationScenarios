namespace OrchestrationScenarios.Models.ContentParts;

public class FunctionCallContent(string pluginName, string functionName, string callId, string arguments) : ContentPart
{
    public string PluginName { get; set; } = pluginName;
    public string FunctionName { get; set; } = functionName;
    public string CallId { get; set; } = callId;
    public string Arguments { get; set; } = arguments;
}
