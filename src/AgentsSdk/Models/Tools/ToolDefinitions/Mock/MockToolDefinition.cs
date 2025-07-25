using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentsSdk.Conversion;
using Microsoft.Extensions.AI;

namespace AgentsSdk.Models.Tools.ToolDefinitions.Mock;

public class MockToolDefinition : ClientSideToolDefinition
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

    public MockToolDefinition(string name, ToolOverrides? overrides)
        : base("function")
    {
        // Remove "Mock." from the beginning of the name
        Name = name.StartsWith("Mock.", StringComparison.OrdinalIgnoreCase)
            ? name[5..]
            : name;
        Overrides = overrides;
        _baseParameters = overrides!.Parameters;
        Description = overrides!.Description;
        

        Method = async (JsonNode input) =>
        {
            await Task.Delay(1); // Simulate some async work
            return "success";
        };

        _executor = async (inputDict) =>
        {
            var normalized = ToolArgumentNormalizer.NormalizeArguments(Parameters, inputDict);
            var inputNode = new JsonObject();
            foreach (var (key, value) in normalized)
            {
                inputNode[key] = JsonSerializer.SerializeToNode(value);
            }

            var result = Method!.DynamicInvoke(inputNode);
            return result is Task taskResult ? await ConvertAsync(taskResult) : result;
        };
    }
}
