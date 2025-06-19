namespace OrchestrationScenarios.Models.ContentParts;

using System.Text;

public class FunctionCallContent(string pluginName, string functionName, string callId, string? arguments = null) : ContentPart
{
    public string PluginName { get; set; } = pluginName;
    public string FunctionName { get; set; } = functionName;
    public string CallId { get; set; } = callId;
    public string Arguments { get; set; } = arguments ?? string.Empty;

    // Streaming-related properties
    public int FunctionCallIndex { get; set; }
    public StringBuilder StreamingArguments { get; } = new();
    public object? Results { get; set; }

    /// <summary>
    /// Gets the full name in "plugin-function" format.
    /// </summary>
    public string Name => $"{PluginName}-{FunctionName}";

    /// <summary>
    /// Parses the plugin and function name from FunctionName assuming "plugin-function" format.
    /// </summary>
    public (string PluginName, string FunctionName) ParsePluginAndFunction()
    {
        var parts = FunctionName.Split('-');
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Expected FunctionName in format 'plugin-function', got '{FunctionName}'.");
        }

        return (parts[0], parts[1]);
    }

    /// <summary>
    /// Alternate constructor that accepts name in "plugin-function" format.
    /// </summary>
    public FunctionCallContent(string name, string callId, string? arguments = null, int functionCallIndex = 0)
        : this(
            pluginName: name.Split('-')[0],
            functionName: name.Split('-')[1],
            callId: callId,
            arguments: arguments ?? string.Empty)
    {
        if (!name.Contains('-'))
        {
            throw new ArgumentException($"Expected name in format 'plugin-function', got '{name}'.", nameof(name));
        }

        FunctionCallIndex = functionCallIndex;
    }
}
