using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System.Text.Json;

namespace FlowSynchronizer;

public class FlowSynchronizerService
{
    private readonly DataverseSettings _dataverse;

    public FlowSynchronizerService(IOptions<DataverseSettings> dataverseOptions)
    {
        _dataverse = dataverseOptions.Value;
    }

    public async Task RunAsync()
    {
        var svc = ConnectToDataverseAsync();
        Console.WriteLine("Connected to Dataverse.");
        await SyncFlowsToSolution(svc);
        Console.WriteLine("All flows synchronized.");
    }

    private ServiceClient ConnectToDataverseAsync()
    {
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
    }

    private async Task SyncFlowsToSolution(ServiceClient svc)
    {
        var solutionFlows = GetFlowsInSolution(svc);
        var flowFiles = Directory.GetFiles("Resources/Flows", "*.json")
            .Select(path => (Path.GetFileNameWithoutExtension(path), path, File.GetLastWriteTimeUtc(path)))
            .ToList();

        foreach (var (flowName, path, lastModified) in flowFiles)
        {
            var json = await File.ReadAllTextAsync(path);
            if (solutionFlows.TryGetValue(flowName, out var flowInfo))
            {
                if (lastModified <= flowInfo.modifiedOn)
                {
                    Console.WriteLine($"No change: {flowName} ({flowInfo.flowId})");
                    continue;
                }

                svc.Update(new Entity("workflow", flowInfo.flowId) { ["clientdata"] = json });
                Console.WriteLine($"Updated: {flowName} (ID: {flowInfo.flowId})");
            }
            else
            {
                var newId = CreateOrUpdateFlow(svc, flowName, json, _dataverse.SolutionUniqueName);
                ActivateFlow(svc, newId);
                Console.WriteLine($"Created & Activated: {flowName} ({newId})");
            }
        }

        foreach (var (flowName, (flowId, _)) in solutionFlows)
        {
            if (!flowFiles.Any(f => f.Item1.Equals(flowName, StringComparison.OrdinalIgnoreCase)))
            {
                svc.Delete("workflow", flowId);
                Console.WriteLine($"Deleted: {flowName} ({flowId})");
            }
        }
    }

    private Dictionary<string, (Guid flowId, DateTime modifiedOn)> GetFlowsInSolution(ServiceClient svc)
    {
        // First get solution ID
        var solutionQuery = new QueryExpression("solution")
        {
            ColumnSet = new ColumnSet("solutionid"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("uniquename", ConditionOperator.Equal, _dataverse.SolutionUniqueName)
                }
            }
        };

        var solutionResult = svc.RetrieveMultiple(solutionQuery);
        if (solutionResult.Entities.Count == 0)
            throw new Exception($"Solution '{_dataverse.SolutionUniqueName}' not found.");

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


    private static Guid CreateOrUpdateFlow(ServiceClient svc, string flowName, string clientDataJson, string solutionUniqueName)
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

    private static void ActivateFlow(ServiceClient svc, Guid workflowId)
    {
        var activate = new Entity("workflow", workflowId)
        {
            ["statecode"] = new OptionSetValue(1)
        };
        svc.Update(activate);
    }

    private static async Task<string> GetFlowCallbackUrlAsync(string flowId)
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
