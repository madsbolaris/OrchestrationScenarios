namespace OrchestrationScenarios.Models;

using System.Text.Json;
using OpenAI.Responses;
using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Messages.Content;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.Deltas.Content;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;

public abstract class Agent(OpenAIResponseClient client)
{
    public string Model { get; set; } = "gpt-4";
    public List<ChatMessage> Preamble { get; set; } = [];
    public List<ChatMessage> Conclusion { get; set; } = [];
    public List<Tool> Tools { get; set; } = [];

    public async IAsyncEnumerable<StreamingUpdate> StreamRunAsync(List<ChatMessage> messages)
    {
        var aiFunctions = ToolFactory.CreateAIFunctions();
        var tools = aiFunctions.Select((keyValuePair) =>
        {
            var function = keyValuePair.Value;
            var jsonSchema = function.JsonSchema;

            if (!jsonSchema.TryGetProperty("properties", out JsonElement propertiesElement))
            {
                throw new InvalidOperationException("The JSON schema does not contain a 'properties' section.");
            }

            // Convert the 'properties' JsonElement to BinaryData
            var propertiesBinaryData = BinaryData.FromString(propertiesElement.GetRawText());

            // Use in the tool creation
            return ResponseTool.CreateFunctionTool(
                function.Name,
                function.Description,
                functionParameters: propertiesBinaryData
            );
        }).ToList();
        tools.Add(ResponseTool.CreateWebSearchTool());

        var handler = new ResponseStreamHandler(client);
        var didRunComplete = false;

        while (didRunComplete == false)
        {
            var callIds = new HashSet<string>();
            var completedCallIds = new HashSet<string>();

            await foreach (var part in handler.RunStreamingAsync(messages, tools, aiFunctions))
            {
                yield return part;

                if (part is RunUpdate endOp && endOp.Delta is EndStreamingOperation<RunDelta> endStreamingOp)
                {
                    didRunComplete = true;
                    continue;
                }
            }
        }
    }
}
