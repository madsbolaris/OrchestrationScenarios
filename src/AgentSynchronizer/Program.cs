using AgentSynchronizer;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;

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
        services.AddSingleton<AgentSynchronizerService>();
        services.AddSingleton((sp) =>
        {
            var _dataverse = sp.GetRequiredService<IOptions<DataverseSettings>>().Value;
            var credential = new ClientSecretCredential(_dataverse.TenantId, _dataverse.ClientId, _dataverse.ClientSecret);

            async Task<string> TokenProvider(string resourceUrl)
            {
                var resource = new Uri(resourceUrl).GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped);
                var token = await credential.GetTokenAsync(new TokenRequestContext([ $"{resource}/.default" ]));
                return token.Token;
            }

            var svc = new ServiceClient(tokenProviderFunction: TokenProvider, instanceUrl: new Uri(_dataverse.EnvironmentUrl));
            if (!svc.IsReady)
                throw new Exception("Failed to connect to Dataverse.");
            return svc;
        });
    })
    .Build();

var service = host.Services.GetRequiredService<AgentSynchronizerService>();

await service.RunAsync();
