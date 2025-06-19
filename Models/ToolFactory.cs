namespace OrchestrationScenarios.Models;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenAI.Responses;

public static class ToolFactory
{
    public static List<ResponseTool> CreateAll(Kernel kernel)
    {
        var tools = new List<ResponseTool>
        {
            // Add default web search tool
            ResponseTool.CreateWebSearchTool()
        };

        // Add kernel plugin tools (e.g., DateTime)
        var dateTimePlugin = kernel.Plugins["DateTime"];
        if (dateTimePlugin is not null)
        {
            foreach (var function in dateTimePlugin)
            {
                tools.Add(function.ToResponseTool("DateTime"));
            }
        }

        return tools;
    }
}
