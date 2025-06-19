namespace OrchestrationScenarios.Models.ContentParts;

public class StreamedFunctionCallContent : StreamedContentPart
{
    public string? PluginName { get; }
    public string? FunctionName { get; }
    public string? CallId { get; }
    public string ArgumentDelta { get; }

    // Main constructor
    public StreamedFunctionCallContent(
        string messageId,
        int index,
        string? callId,
        string? pluginName,
        string? functionName,
        string? argumentDelta = null)
        : base(AuthorRole.Agent, messageId, index)
    {
        PluginName = pluginName;
        FunctionName = functionName;
        CallId = callId;
        ArgumentDelta = argumentDelta ?? string.Empty;
    }

    // Convenience override with combined name string
    public StreamedFunctionCallContent(
        string messageId,
        int index,
        string? callId,
        string name,
        string? argumentDelta = null)
        : base(AuthorRole.Agent, messageId, index)
    {
        var parts = name.Split('-');
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Expected function name in format 'plugin-function', but got '{name}'.");
        }

        PluginName = parts[0];
        FunctionName = parts[1];
        CallId = callId;
        ArgumentDelta = argumentDelta ?? string.Empty;
    }

    // Minimal constructor for raw deltas (plugin/function unknown)
    public StreamedFunctionCallContent(
        string messageId,
        int index,
        string argumentDelta)
        : base(AuthorRole.Agent, messageId, index)
    {
        ArgumentDelta = argumentDelta;
    }
}
