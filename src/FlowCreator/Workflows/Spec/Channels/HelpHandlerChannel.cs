

using System.Text.Json;
using Microsoft.SemanticKernel;

namespace FlowCreator.Workflows.Spec.Channels;

public class HelpHandlerChannel : IExternalKernelProcessMessageChannel
{
    private List<string> _errors = [];
    private List<string> _helpContent = [];

    public Task EmitExternalEventAsync(string externalTopicEvent, KernelProcessProxyMessage message)
    {
        switch (externalTopicEvent)
        {
            case nameof(SpecWorkflowExternalTopics.RelayHelp):
                if (message.EventData!.Content is string helpMessage)
                {
                    _helpContent.Add(JsonSerializer.Deserialize<string>(helpMessage)!);
                }
                break;

            case nameof(SpecWorkflowExternalTopics.RelayError):
                if (message.EventData!.Content is string errorMessage)
                {
                    _errors.Add(JsonSerializer.Deserialize<string>(errorMessage)!);
                }
                break;

            default:
                throw new NotSupportedException($"External topic '{externalTopicEvent}' is not supported.");
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetErrors() => _errors.AsReadOnly();
    public IReadOnlyList<string> GetHelpContent() => _helpContent.AsReadOnly();

    public ValueTask Initialize()
    {
        _errors = new List<string>();
        _helpContent = new List<string>();
        return ValueTask.CompletedTask;
    }

    public ValueTask Uninitialize()
    {
        _errors.Clear();
        _helpContent.Clear();
        return ValueTask.CompletedTask;
    }
}