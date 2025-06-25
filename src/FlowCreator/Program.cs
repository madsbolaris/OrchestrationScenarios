using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.Text.Json;

namespace FlowCreator;

class Program
{
    static async Task Main(string[] args)
    {
        var config = LoadConfig();
        var environmentUrl     = config["EnvironmentUrl"]!;
        var clientId           = config["ClientId"]!;
        var clientSecret       = config["ClientSecret"]!;
        var tenantId           = config["TenantId"]!;
        var solutionUniqueName = config["SolutionUniqueName"]!;

        var serviceClient = ConnectToDataverse(environmentUrl, tenantId, clientId, clientSecret);
        Console.WriteLine("Connected to Dataverse.");

        await SyncFlowsToSolution(serviceClient, solutionUniqueName);

        Console.WriteLine("All flows synchronized.");
    }

    static IConfiguration LoadConfig()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
    }

    static ServiceClient ConnectToDataverse(string environmentUrl, string tenantId, string clientId, string clientSecret)
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        async Task<string> TokenProvider(string resourceUrl)
        {
            var resource = new Uri(resourceUrl).GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped);
            var trc = new TokenRequestContext([$"{resource}/.default"]);
            var token = await credential.GetTokenAsync(trc);
            return token.Token;
        }

        var svc = new ServiceClient(tokenProviderFunction: TokenProvider, instanceUrl: new Uri(environmentUrl));
        if (!svc.IsReady)
            throw new Exception("Failed to connect to Dataverse.");
        return svc;
    }

    static async Task SyncFlowsToSolution(ServiceClient svc, string solutionUniqueName)
    {
        var solutionFlows = GetFlowsInSolution(svc, solutionUniqueName);
        var flowFiles = Directory.GetFiles("Resources/Flows", "*.json")
            .Select(path => (Path.GetFileNameWithoutExtension(path), path, lastModified: File.GetLastWriteTimeUtc(path)))
            .ToList();

        var knownFlowNames = new HashSet<string>(solutionFlows.Keys, StringComparer.OrdinalIgnoreCase);

        // Create or update
        foreach (var (flowName, path, lastModified) in flowFiles)
        {
            var json = await File.ReadAllTextAsync(path);

            if (solutionFlows.TryGetValue(flowName, out var flowInfo))
            {
                var (flowId, modifiedOn) = flowInfo;

                if (lastModified <= modifiedOn)
                {
                    Console.WriteLine($"No change: {flowName} ({flowId})");
                    continue;
                }

                // Get the callback URL for the flow
                svc.Update(new Entity("workflow", flowId) { ["clientdata"] = json });
                Console.WriteLine($"Updating: {flowName} (ID: {flowId})");
            }
            else
            {
                Console.Write($"Creating: {flowName}");
                var newId = CreateOrUpdateFlow(svc, flowName, json, solutionUniqueName);
                Console.Write($" ({newId})");
                ActivateFlow(svc, newId);
                Console.WriteLine(" [Activated]");
            }
        }

        // Delete missing
        foreach (var (flowName, (flowId, _)) in solutionFlows)
        {
            if (!flowFiles.Any(f => f.Item1.Equals(flowName, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"Deleting: {flowName} ({flowId})");
                svc.Delete("workflow", flowId);
            }
        }
    }

    static Dictionary<string, (Guid flowId, DateTime modifiedOn)> GetFlowsInSolution(ServiceClient svc, string solutionUniqueName)
    {
        // First get solution ID
        var solutionQuery = new QueryExpression("solution")
        {
            ColumnSet = new ColumnSet("solutionid"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("uniquename", ConditionOperator.Equal, solutionUniqueName)
                }
            }
        };

        var solutionResult = svc.RetrieveMultiple(solutionQuery);
        if (solutionResult.Entities.Count == 0)
            throw new Exception($"Solution '{solutionUniqueName}' not found.");

        var solutionId = solutionResult.Entities[0].Id;

        // Get all workflow components (ComponentType 29) in the solution
        var componentQuery = new QueryExpression("solutioncomponent")
        {
            ColumnSet = new ColumnSet("objectid"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId),
                    new ConditionExpression("componenttype", ConditionOperator.Equal, 29) // 29 = workflow
                }
            }
        };

        var componentResult = svc.RetrieveMultiple(componentQuery);
        var flowIds = componentResult.Entities.Select(e => (Guid)e["objectid"]).ToList();
        if (flowIds.Count == 0)
            return new Dictionary<string, (Guid, DateTime)>();

        // Retrieve workflows by ID
        var query = new QueryExpression("workflow")
        {
            ColumnSet = new ColumnSet("workflowid", "name", "modifiedon"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("workflowid", ConditionOperator.In, flowIds.Cast<object>().ToArray())
                }
            }
        };

        var result = svc.RetrieveMultiple(query);
        return result.Entities
            .Where(e => e.Contains("name"))
            .ToDictionary(
                e => (string)e["name"],
                e => (
                    (Guid)e["workflowid"],
                    e.Contains("modifiedon") ? ((DateTime)e["modifiedon"]).ToUniversalTime() : DateTime.MinValue
                ),
                StringComparer.OrdinalIgnoreCase
            );
    }


    static Guid CreateOrUpdateFlow(ServiceClient svc, string flowName, string clientDataJson, string solutionUniqueName)
    {
        var query = new QueryExpression("workflow")
        {
            ColumnSet = new ColumnSet("workflowid"),
            Criteria =
            {
                Conditions = { new ConditionExpression("name", ConditionOperator.Equal, flowName) }
            }
        };

        var existing = svc.RetrieveMultiple(query);
        if (existing.Entities.Count > 0)
        {
            var existingFlow = existing.Entities[0];
            svc.Update(new Entity("workflow", existingFlow.Id) { ["clientdata"] = clientDataJson });
            Console.WriteLine($"Updated existing flow: {existingFlow.Id}");
            return existingFlow.Id;
        }
        else
        {
            var newFlow = new Entity("workflow")
            {
                ["category"] = new OptionSetValue(5),
                ["name"] = flowName,
                ["type"] = new OptionSetValue(1),
                ["primaryentity"] = "none",
                ["clientdata"] = clientDataJson
            };

            var newFlowId = svc.Create(newFlow); 

            var addReq = new AddSolutionComponentRequest
            {
                ComponentType = 29,
                ComponentId = newFlowId,
                SolutionUniqueName = solutionUniqueName
            };

            svc.Execute(addReq);

            return newFlowId;
        }
    }

    static void ActivateFlow(ServiceClient svc, Guid workflowId)
    {
        var activate = new Entity("workflow", workflowId)
        {
            ["statecode"] = new OptionSetValue(1)
        };
        svc.Update(activate);
    }

    static async Task<string> GetFlowCallbackUrlAsync(string flowId)
    {
        var scopes = "https://service.flow.microsoft.com/.default";
        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(new TokenRequestContext([scopes]));
        var accessToken = token.Token;

        string url = $"https://us.api.flow.microsoft.com/providers/Microsoft.ProcessSimple/environments/5a7d35b7-29b3-e0ec-a289-8fd0ce3aab0a/flows/{flowId}/triggers/manual/listCallbackUrl?api-version=2016-11-01";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsync(url, null);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("response").GetProperty("value").GetString()!;
    }
}
