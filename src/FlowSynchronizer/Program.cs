using FlowSynchronizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config
            .SetBasePath(Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "..")))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<DataverseSettings>(context.Configuration.GetSection("Dataverse"));
        services.AddSingleton<FlowSynchronizerService>();
    })
    .Build();

var service = host.Services.GetRequiredService<FlowSynchronizerService>();

await service.RunAsync();
