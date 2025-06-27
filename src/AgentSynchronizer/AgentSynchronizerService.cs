using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace AgentSynchronizer;

public class AgentSynchronizerService(ServiceClient svc, IOptions<DataverseSettings> dataverseOptions)
{
    private readonly DataverseSettings _dataverseSettings = dataverseOptions.Value;

    public async Task RunAsync()
    {
        Console.WriteLine("Connected to Dataverse.");

        var botId = await CreateBotAsync();
        var topicId = await CreateTopicAsync(botId);

        var connectionReferenceId = GetConnectionReferenceIdByLogicalName("mabolan_ExcelOnlineBusiness");
        await AssociateConnectionReferenceAsync(topicId, connectionReferenceId);

        await AddToSolutionAsync(botId, 10185);     // bot component
        await AddToSolutionAsync(topicId, 10186);   // botcomponent component

        Console.WriteLine("Bot and topic created and added to solution.");
    }

    private async Task<Guid> CreateBotAsync()
    {
        var bot = new Entity("bot")
        {
            ["schemaname"] = $"{_dataverseSettings.Prefix}_Agent-ExcelOnlineBusiness-GetARow",
            ["name"] = "Agent-ExcelOnlineBusiness-GetARow",
            ["language"] = new OptionSetValue(1033),
            ["authenticationmode"] = new OptionSetValue(2),
            ["authenticationtrigger"] = new OptionSetValue(1),
            ["template"] = "default-2.1.0",
            ["configuration"] = File.ReadAllText("Resources/Agents/ExcelOnlineBusiness-GetARow/configuration.json")
        };

        return await svc.CreateAsync(bot);
    }

    private async Task<Guid> CreateTopicAsync(Guid botId)
    {
        var yaml = File.ReadAllText("Resources/Agents/ExcelOnlineBusiness-GetARow/ExcelOnlineBusiness-GetARow.yaml");

        var topic = new Entity("botcomponent")
        {
            ["schemaname"] = $"{_dataverseSettings.Prefix}_Agent-ExcelOnlineBusiness-GetARow.ExcelOnlineBusiness-GetARow",
            ["name"] = "Agent-ExcelOnlineBusiness-GetARow.ExcelOnlineBusiness-GetARow",
            ["componenttype"] = new OptionSetValue(9), // Topic (V2)
            ["parentbotid"] = new EntityReference("bot", botId),
            ["language"] = new OptionSetValue(1033),
            ["data"] = yaml,
            ["statecode"] = new OptionSetValue(0), // Active
            ["statuscode"] = new OptionSetValue(1)
        };

        return await svc.CreateAsync(topic);
    }

    private Guid GetConnectionReferenceIdByLogicalName(string logicalName)
    {
        var query = new QueryExpression("connectionreference")
        {
            ColumnSet = new ColumnSet("connectionreferenceid"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("connectionreferencelogicalname", ConditionOperator.Equal, logicalName)
                }
            }
        };

        var results = svc.RetrieveMultiple(query);
        if (results.Entities.Count == 0)
        {
            throw new InvalidOperationException($"No connection reference found with logical name '{logicalName}'");
        }

        return results.Entities[0].Id;
    }


    private async Task AssociateConnectionReferenceAsync(Guid botComponentId, Guid connectionReferenceId)
    {
        var botComponentRef = new EntityReference("botcomponent", botComponentId);
        var connectionRef = new EntityReference("connectionreference", connectionReferenceId);

        var relationship = new Relationship("botcomponent_connectionreference");

        await svc.AssociateAsync(
            botComponentRef.LogicalName,
            botComponentRef.Id,
            relationship,
            [connectionRef]);

        Console.WriteLine($"Associated botcomponent {botComponentId} with connectionreference {connectionReferenceId}");
    }


    private async Task AddToSolutionAsync(Guid id, int componentType)
    {
        var addReq = new AddSolutionComponentRequest
        {
            ComponentId = id,
            ComponentType = componentType,
            SolutionUniqueName = _dataverseSettings.AgentSolution
        };

        await svc.ExecuteAsync(addReq);
    }


}
