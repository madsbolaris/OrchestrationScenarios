using System.Text.Json;
using FlowCreator.Models;

namespace FlowCreator.Services;

public class WorkingFlowDefinitionService
{
    private FlowDefinition _currentFlowDefinition = new();

    public FlowDefinition UpdateCurrentFlowDefinition(Func<FlowDefinition, FlowDefinition> factory)
    {
        _currentFlowDefinition = factory(_currentFlowDefinition);

        return _currentFlowDefinition;
    }

    public FlowDefinition GetCurrentFlowDefinition()
    {
        return _currentFlowDefinition;
    }
}
