using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using YamlDotNet.RepresentationModel;

namespace AgentSynchronizer;

public class AgentSynchronizerService(ServiceClient svc, IOptions<DataverseSettings> dataverseOptions)
{
    private readonly DataverseSettings _dataverseSettings = dataverseOptions.Value;

    public async Task RunAsync()
    {
        await SyncAgentsToSolutionAsync();
        Console.WriteLine("All agents synchronized.");
    }

    private async Task SyncAgentsToSolutionAsync()
    {
        var localAgents = Directory.GetDirectories("Resources/Agents")
            .Select(path => new
            {
                Name = "Agent-"+Path.GetFileName(path)!,
                Path = path,
                ModifiedOn = Directory
                    .EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Select(File.GetLastWriteTimeUtc)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max()
            })
            .ToList();

        var solutionBots = await GetBotsInSolutionAsync();

        foreach (var agent in localAgents)
        {
            if (solutionBots.TryGetValue(agent.Name, out var existing))
            {
                if (agent.ModifiedOn <= existing.modifiedOn)
                {
                    Console.WriteLine($"No change: {agent.Name} ({existing.botId})");
                    continue;
                }

                try
                {
                    var updatedId = await CreateOrUpdateAgentAsync(agent.Name, agent.Path, existing.botId);
                    Console.WriteLine($"Updated: {agent.Name} ({updatedId})");
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    Console.WriteLine($"Error updating agent '{agent.Name}': {ex.Message}");
                    continue;
                }
            }
            else
            {
                try
                {
                    var newId = await CreateOrUpdateAgentAsync(agent.Name, agent.Path, null);
                    Console.WriteLine($"Created: {agent.Name} ({newId})");
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    Console.WriteLine($"Error creating agent '{agent.Name}': {ex.Message}");
                    continue;
                }
            }
        }

        foreach (var (name, (botId, _)) in solutionBots)
        {
            if (!localAgents.Any(agent => agent.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                // Delete all botcomponent children first
                var topicQuery = new QueryExpression("botcomponent")
                {
                    ColumnSet = new ColumnSet("botcomponentid"),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression("parentbotid", ConditionOperator.Equal, botId)
                        }
                    }
                };

                var topicResults = await svc.RetrieveMultipleAsync(topicQuery);
                foreach (var topic in topicResults.Entities)
                {
                    var topicId = topic.Id;
                    await svc.DeleteAsync("botcomponent", topicId);
                    Console.WriteLine($"Deleted topic: {topicId}");
                }

                await svc.DeleteAsync("bot", botId);
                Console.WriteLine($"Deleted: {name} ({botId})");
            }
        }

    }

    private async Task<Dictionary<string, (Guid botId, DateTime modifiedOn)>> GetBotsInSolutionAsync()
    {
        var solutionQuery = new QueryExpression("solution")
        {
            ColumnSet = new ColumnSet("solutionid"),
            Criteria = { Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, _dataverseSettings.AgentSolution) } }
        };

        var solutionResult = await svc.RetrieveMultipleAsync(solutionQuery);
        if (solutionResult.Entities.Count == 0)
            throw new Exception($"Solution '{_dataverseSettings.AgentSolution}' not found.");

        var solutionId = solutionResult.Entities[0].Id;

        var componentQuery = new QueryExpression("solutioncomponent")
        {
            ColumnSet = new ColumnSet("objectid"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId),
                    new ConditionExpression("componenttype", ConditionOperator.Equal, 10185) // bot
                }
            }
        };

        var componentResult = await svc.RetrieveMultipleAsync(componentQuery);
        var botIds = componentResult.Entities.Select(e => (Guid)e["objectid"]).ToList();

        if (botIds.Count == 0)
            return [];

        var botQuery = new QueryExpression("bot")
        {
            ColumnSet = new ColumnSet("botid", "schemaname", "modifiedon"),
            Criteria = { Conditions = { new ConditionExpression("botid", ConditionOperator.In, botIds.Cast<object>().ToArray()) } }
        };

        var bots = await svc.RetrieveMultipleAsync(botQuery);
        return bots.Entities
            .Where(e => e.Contains("schemaname"))
            .ToDictionary(
                e => ((string)e["schemaname"]).Split('_').Last(),
                e => ((Guid)e["botid"], e.GetAttributeValue<DateTime?>("modifiedon")?.ToUniversalTime() ?? DateTime.MinValue),
                StringComparer.OrdinalIgnoreCase
            );
    }

    private async Task<Guid> CreateOrUpdateAgentAsync(string agentName, string folderPath, Guid? existingBotId)
    {
        var prefix = _dataverseSettings.Prefix;
        var schemaName = $"{prefix}_Agent-{agentName}";
        var botName = $"Agent-{agentName}";
        var configPath = Path.Combine(folderPath, "configuration.json");
        var yamlPath = Directory.GetFiles(folderPath, "*.yaml").FirstOrDefault() ?? throw new FileNotFoundException("YAML file not found.");

        var config = await File.ReadAllTextAsync(configPath);
        var yaml = await File.ReadAllTextAsync(yamlPath);

        var bot = new Entity("bot")
        {
            ["schemaname"] = schemaName,
            ["name"] = botName,
            ["language"] = new OptionSetValue(1033),
            ["authenticationmode"] = new OptionSetValue(2),
            ["authenticationtrigger"] = new OptionSetValue(1),
            ["template"] = "default-2.1.0",
            ["configuration"] = config
        };

        Guid botId;
        if (existingBotId.HasValue)
        {
            bot.Id = existingBotId.Value;
            await svc.UpdateAsync(bot);
            botId = bot.Id;
        }
        else
        {
            botId = await svc.CreateAsync(bot);
            await svc.ExecuteAsync(new AddSolutionComponentRequest
            {
                ComponentId = botId,
                ComponentType = 10185,
                SolutionUniqueName = _dataverseSettings.AgentSolution
            });
        }

        // Upsert botcomponent (topic)
        var topicSchema = $"{schemaName}.{agentName}";
        var topicName = $"{botName}.{agentName}";
        var topicQuery = new QueryExpression("botcomponent")
        {
            ColumnSet = new ColumnSet("botcomponentid"),
            Criteria = { Conditions = { new ConditionExpression("schemaname", ConditionOperator.Equal, topicSchema) } }
        };

        var topicResult = await svc.RetrieveMultipleAsync(topicQuery);
        var topic = new Entity("botcomponent")
        {
            ["schemaname"] = topicSchema,
            ["name"] = topicName,
            ["componenttype"] = new OptionSetValue(9),
            ["parentbotid"] = new EntityReference("bot", botId),
            ["language"] = new OptionSetValue(1033),
            ["data"] = yaml,
            ["statecode"] = new OptionSetValue(0),
            ["statuscode"] = new OptionSetValue(1)
        };

        Guid topicId;
        if (topicResult.Entities.Count > 0)
        {
            topic.Id = topicResult.Entities[0].Id;
            await svc.UpdateAsync(topic);
            topicId = topic.Id;
        }
        else
        {
            topicId = await svc.CreateAsync(topic);
            await svc.ExecuteAsync(new AddSolutionComponentRequest
            {
                ComponentId = topicId,
                ComponentType = 10186,
                SolutionUniqueName = _dataverseSettings.AgentSolution
            });
        }

        // Associate connection reference
        var logicalName = GetConnectionReferenceLogicalName(yaml);
        var connId = GetConnectionReferenceIdByLogicalName(logicalName);

        var relationship = new Relationship("botcomponent_connectionreference");
        await svc.AssociateAsync("botcomponent", topicId, relationship, [new EntityReference("connectionreference", connId)]);

        return botId;
    }

    private Guid GetConnectionReferenceIdByLogicalName(string logicalName)
    {
        var query = new QueryExpression("connectionreference")
        {
            ColumnSet = new ColumnSet("connectionreferenceid"),
            Criteria = { Conditions = { new ConditionExpression("connectionreferencelogicalname", ConditionOperator.Equal, logicalName) } }
        };

        var results = svc.RetrieveMultiple(query);
        if (results.Entities.Count == 0)
            throw new InvalidOperationException($"No connection reference found with logical name '{logicalName}'");

        return results.Entities[0].Id;
    }

    private static string GetConnectionReferenceLogicalName(string yamlContent)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(yamlContent));

        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        if (root.Children.TryGetValue("action", out var actionNode) &&
            actionNode is YamlMappingNode actionMapping &&
            actionMapping.Children.TryGetValue("connectionReference", out var logicalNameNode))
        {
            return logicalNameNode.ToString();
        }

        throw new InvalidOperationException("connectionReference not found in YAML.");
    }

}
