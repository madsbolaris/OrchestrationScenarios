using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentsSdk.Conversion;
using Microsoft.Extensions.AI;

namespace AgentsSdk.Models.Tools.ToolDefinitions.Function;

public class FunctionToolDefinition : ClientSideToolDefinition
{
    public bool? Strict { get; set; }

    private Delegate? _method;

    public Delegate? Method
    {
        get => _method;
        set
        {
            _method = value;
        }
    }

    public FunctionToolDefinition(string name, string? description = null)
        : base("function")
    {
        Name = name;
        Description = description;
    }

    public static FunctionToolDefinition CreateToolDefinitionFromObjectMethod<T>(T instance, string methodName)
    {
        var methodInfo = typeof(T).GetMethod(methodName)!;

        var aiFunction = AIFunctionFactory.Create(methodInfo, instance);
        var baseParameters = JsonNode.Parse(aiFunction.JsonSchema.GetRawText());

        var tool = new FunctionToolDefinition(aiFunction.Name, aiFunction.Description)
        {
            Method = aiFunction.InvokeAsync,
            _baseParameters = baseParameters
        };

        tool._executor = async (inputDict) =>
        {
            var effectiveSchema = tool.Overrides?.Parameters ?? tool.Parameters;
            var normalized = ToolArgumentNormalizer.NormalizeArguments(effectiveSchema, inputDict);

            return await aiFunction.InvokeAsync(new(normalized));
        };


        return tool;
    }
}
