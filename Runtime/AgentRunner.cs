using OpenAI.Responses;
using OrchestrationScenarios.Conversion;
using OrchestrationScenarios.Execution;
using OrchestrationScenarios.Models.Messages;
using OrchestrationScenarios.Models.Messages.Types;
using OrchestrationScenarios.Models.Runs.Responses.Deltas;
using OrchestrationScenarios.Models.Runs.Responses.StreamingOperations;
using OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates;
using OrchestrationScenarios.Utilities;
using StreamingUpdate = OrchestrationScenarios.Models.Runs.Responses.StreamingUpdates.StreamingUpdate;

namespace OrchestrationScenarios.Runtime
{
    public static class AgentRunner
    {
        public static async Task RunAsync(OpenAIResponseClient client, List<ChatMessage> allMessages)
        {
            int agentIndex = allMessages.FindIndex(m => m is AgentMessage);
            if (agentIndex == -1)
                throw new InvalidOperationException("No agent message found to stream.");

            var inputMessages = allMessages.Take(agentIndex).ToList();
            var stream = GetStreamingUpdates(client, inputMessages);

            await ConsoleRenderHelper.DisplayConversationAsync(inputMessages, stream);
        }

        private static IAsyncEnumerable<StreamingUpdate> GetStreamingUpdates(OpenAIResponseClient client, List<ChatMessage> messages)
        {
            var aiFunctions = ToolFactory.CreateAIFunctions();
            var tools = aiFunctions
                .Select(kvp => MicrosoftExtensionsAIToResponseConverter.Convert(kvp.Value))
                .ToList();

            tools.Add(ResponseTool.CreateWebSearchTool());
            var handler = new ResponseStreamHandler(client);

            return Stream();
            async IAsyncEnumerable<StreamingUpdate> Stream()
            {
                var done = false;
                while (!done)
                {
                    await foreach (var part in handler.RunStreamingAsync(messages, tools, aiFunctions))
                    {
                        yield return part;
                        if (part is RunUpdate { Delta: EndStreamingOperation<RunDelta> })
                            done = true;
                    }
                }
            }
        }
    }
}
