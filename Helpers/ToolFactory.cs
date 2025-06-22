using Microsoft.Extensions.AI;

namespace OrchestrationScenarios.Models;


public static class ToolFactory
{
    public static Dictionary<string, AIFunction> CreateAIFunctions()
    {
        var tools = new Dictionary<string, AIFunction>();

        // Add any built-in plugins/functions
        var nowFunction = AIFunctionFactory.Create(
            () => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            "DateTime-Now",
            "Returns the current time in the format yyyy-MM-dd HH:mm:ss"
        );

        tools.Add("DateTime-Now", nowFunction);

        return tools;
    }
}
