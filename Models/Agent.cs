namespace OrchestrationScenarios.Models;

using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI;
using OpenAI.Responses;
using OrchestrationScenarios.Models.ContentParts;

public abstract class Agent(OpenAIResponseClient client)
{
    
    public string Model { get; set; } = "gpt-4";
    public List<Message> Preamble { get; set; } = [];
    public List<Message> Conclusion { get; set; } = [];
    public List<Tool> Tools { get; set; } = [];

    public async IAsyncEnumerable<StreamedContentPart> StreamRunAsync(List<Message> messages)
    {
        // Define the agent
        var agent = new OpenAIResponseAgent(client)
        {
            Name = "ResponseAgent",
            Instructions = "Answer all queries in the user's preferred language.",
        };

        var chatMessages = messages.Select((m) =>
        {
            var contents = new ChatMessageContentItemCollection();
            foreach (var contentPart in m.Content)
            {
                switch (contentPart)
                {
                    case ContentParts.TextContent textContent:
                        contents.Add(new Microsoft.SemanticKernel.TextContent(textContent.Text));
                        break;
                    default:
                        throw new ArgumentException($"Unknown content type: {contentPart.GetType().Name}");
                }
            }

            return new ChatMessageContent(
                role: m.Role switch
                {
                    AuthorRole.User => Microsoft.SemanticKernel.ChatCompletion.AuthorRole.User,
                    AuthorRole.Agent => Microsoft.SemanticKernel.ChatCompletion.AuthorRole.Assistant,
                    AuthorRole.Tool => Microsoft.SemanticKernel.ChatCompletion.AuthorRole.Tool,
                    AuthorRole.Developer => Microsoft.SemanticKernel.ChatCompletion.AuthorRole.Developer,
                    AuthorRole.System => Microsoft.SemanticKernel.ChatCompletion.AuthorRole.System,
                    _ => throw new ArgumentOutOfRangeException($"Unknown role: {m.Role}")
                },
                items: contents
            );
        });

        List<ResponseItem> items = [.. chatMessages.Select(m => m.ToResponseItem())];

        var options = new ResponseCreationOptions
        {
            StoredOutputEnabled = false,
            Tools = { ResponseTool.CreateWebSearchTool() }
        };

        var response = client.CreateResponseStreamingAsync(items, options);

        await foreach (var item in response)
        {
            switch (item)
            {
                case StreamingResponseCreatedUpdate createdUpdate:
                    break;

                case StreamingResponseOutputItemAddedUpdate outputItemAddedUpdate:
                    break;
                case StreamingResponseContentPartAddedUpdate contentPartAddedUpdate:
                    yield return new StreamedStartContent(AuthorRole.Agent, contentPartAddedUpdate.ItemId);
                    break;

                case StreamingResponseOutputTextDeltaUpdate textDeltaUpdate:
                    yield return new StreamedTextContent(AuthorRole.Agent, textDeltaUpdate.ItemId, textDeltaUpdate.ContentIndex, textDeltaUpdate.Delta);
                    break;

                case StreamingResponseContentPartDoneUpdate contentPartAddedUpdate:
                    yield return new StreamedEndContent(AuthorRole.Agent, contentPartAddedUpdate.ItemId, contentPartAddedUpdate.ContentIndex+1);
                    break;

                case StreamingResponseWebSearchCallInProgressUpdate webSearchInProgressUpdate:
                    yield return new StreamedStartContent(AuthorRole.Agent, webSearchInProgressUpdate.ItemId);
                    yield return new StreamedTextContent(AuthorRole.Agent, webSearchInProgressUpdate.ItemId, 0, "WebSearch.Search()");
                    yield return new StreamedEndContent(AuthorRole.Agent, webSearchInProgressUpdate.ItemId, 1); 

                    yield return new StreamedStartContent(AuthorRole.Tool, "tool-" + webSearchInProgressUpdate.ItemId);
                    break;

                case StreamingResponseWebSearchCallSearchingUpdate webSearchSearchingUpdate:
                    break;

                case StreamingResponseWebSearchCallCompletedUpdate webSearchCompletedUpdate:
                    yield return new StreamedTextContent(AuthorRole.Tool, "tool-"+webSearchCompletedUpdate.ItemId, 0, "REDACTED");
                    yield return new StreamedEndContent(AuthorRole.Tool, "tool-"+webSearchCompletedUpdate.ItemId, 1);
                    break;

                case StreamingResponseOutputItemDoneUpdate doneUpdate:
                    break;
            }
        }
    }
}
