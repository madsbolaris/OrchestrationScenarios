namespace OrchestrationScenarios.Models;

using OrchestrationScenarios.Models.ContentParts;

public abstract class Agent(OpenAIResponseClient client)
{
    public string Model { get; set; } = "gpt-4";
    public List<Message> Preamble { get; set; } = [];
    public List<Message> Conclusion { get; set; } = [];
    public List<Tool> Tools { get; set; } = [];

    public async IAsyncEnumerable<StreamedContentPart> StreamRunAsync(List<Message> messages)
    {
        var kernel = KernelFactory.Create();
        var tools = ToolFactory.CreateAll(kernel);
        var handler = new ResponseStreamHandler(client, kernel);

        while (true)
        {
            var callIds = new HashSet<string>();
            var completedCallIds = new HashSet<string>();

            await foreach (var part in handler.RunStreamingAsync(messages, tools))
            {
                switch (part)
                {
                    case StreamedFunctionCallContent call:
                        if (call.CallId == null)
                        {
                            callIds.Add(call.CallId);
                        }
                        break;

                    case StreamedFunctionResultContent result:
                        completedCallIds.Add(result.CallId);
                        break;
                }

                yield return part;
            }

            // Exit if all function calls have corresponding results
            var incompleteCalls = callIds.Except(completedCallIds);
            if (!incompleteCalls.Any())
            {
                break;
            }
        }
    }
}
