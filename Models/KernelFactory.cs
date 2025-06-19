namespace OrchestrationScenarios.Models;

using Microsoft.SemanticKernel;

public static class KernelFactory
{
    public static Kernel Create()
    {
        var kernel = new Kernel();

        // Add any built-in plugins/functions
        var nowFunction = KernelFunctionFactory.CreateFromMethod(
            () => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            "Now",
            "Returns the current time in the format yyyy-MM-dd HH:mm:ss"
        );

        var dateTimePlugin = KernelPluginFactory.CreateFromFunctions("DateTime", [nowFunction]);
        kernel.Plugins.Add(dateTimePlugin);

        return kernel;
    }
}
