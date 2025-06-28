using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using YamlDotNet.RepresentationModel;
using System.Text.Json;

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
        var localAgents = Directory.EnumerateFiles("Resources/Agents", "*.yaml", SearchOption.AllDirectories)
            .Select(path => new
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path,
                ModifiedOn = File.GetLastWriteTimeUtc(path)
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

    private async Task<Guid> CreateOrUpdateAgentAsync(string agentName, string yamlPath, Guid? existingBotId)
    {
        var yamlContent = await File.ReadAllTextAsync(yamlPath);
        var yaml = new YamlStream();
        yaml.Load(new StringReader(yamlContent));
        var root = (YamlMappingNode)yaml.Documents[0].RootNode;

        string name = root.Children.ContainsKey(new YamlScalarNode("name"))
            ? ((YamlScalarNode)root.Children[new YamlScalarNode("name")]).Value ?? agentName
            : agentName;

        string description = root.Children.ContainsKey(new YamlScalarNode("description"))
            ? ((YamlScalarNode)root.Children[new YamlScalarNode("description")]).Value ?? ""
            : "";

        string model = root.Children.ContainsKey(new YamlScalarNode("model"))
            ? ((YamlScalarNode)root.Children[new YamlScalarNode("model")]).Value ?? "gpt-4.1"
            : "gpt-4.1";

        // Parse tools
        List<string> toolTypes = new();
        if (root.Children.TryGetValue(new YamlScalarNode("tools"), out var toolsNode) &&
            toolsNode is YamlSequenceNode toolSequence)
        {
            foreach (var toolItem in toolSequence)
            {
                if (toolItem is YamlMappingNode toolMap &&
                    toolMap.Children.TryGetValue(new YamlScalarNode("type"), out var typeNode) &&
                    typeNode is YamlScalarNode typeScalar &&
                    !string.IsNullOrEmpty(typeScalar.Value))
                {
                    toolTypes.Add(typeScalar.Value);
                }
            }
        }


        var prefix = _dataverseSettings.Prefix;
        var schemaName = $"{prefix}_{name}";
        var botName = name;

        var bot = new Entity("bot")
        {
            ["schemaname"] = schemaName,
            ["name"] = botName,
            ["language"] = new OptionSetValue(1033),
            ["authenticationmode"] = new OptionSetValue(2),
            ["authenticationtrigger"] = new OptionSetValue(1),
            ["template"] = "default-2.1.0",
            ["configuration"] = """{ "$kind": "BotConfiguration", "settings": { "GenerativeActionsEnabled": true }, "aISettings": { "$kind": "AISettings", "useModelKnowledge": true, "isFileAnalysisEnabled": true, "isSemanticSearchEnabled": true, "optInUseLatestModels": false }, "recognizer": { "$kind": "GenerativeAIRecognizer" } }"""
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

        // Delete all existing botcomponents for this bot
        var existingTopicsQuery = new QueryExpression("botcomponent")
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

        var existingTopics = await svc.RetrieveMultipleAsync(existingTopicsQuery);
        foreach (var topic in existingTopics.Entities)
        {
            await svc.DeleteAsync("botcomponent", topic.Id);
            Console.WriteLine($"Deleted existing topic before update: {topic.Id}");
        }

        foreach (var toolType in toolTypes)
        {
            var flowPath = Path.Combine("Resources", "Flows", $"{toolType}.json");

            if (!File.Exists(flowPath))
                throw new FileNotFoundException($"Flow definition not found for tool: {toolType}");

            using var stream = File.OpenRead(flowPath);
            using var doc = await JsonDocument.ParseAsync(stream);
            var props = doc.RootElement.GetProperty("properties");

            var summary = props.GetProperty("summary").GetString()!;
            var toolDesc = props.GetProperty("description").GetString()!;
            var connRefs = props.GetProperty("connectionReferences").EnumerateObject().First();
            var connectionLogicalName = connRefs.Value.GetProperty("connection").GetProperty("connectionReferenceLogicalName").GetString()!;
            var operationId = props
                .GetProperty("definition")
                .GetProperty("actions")
                .GetProperty("try")
                .GetProperty("actions")
                .GetProperty("action")
                .GetProperty("inputs")
                .GetProperty("host")
                .GetProperty("operationId")
                .GetString()!;

            var topicSchema = $"{schemaName}.{toolType}";
            var topicName = $"{botName}.{toolType}";

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
                ["data"] = $"""
                    kind: TaskDialog
                    modelDisplayName: {summary}
                    modelDescription: {toolDesc}
                    outputs:
                      - propertyName: Response

                    action:
                        kind: InvokeConnectorTaskAction
                        connectionReference: {connectionLogicalName}
                        connectionProperties:
                            mode: Invoker

                        operationId: {operationId}

                    outputMode: All
                    """,
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

            var connId = GetConnectionReferenceIdByLogicalName(connectionLogicalName);
            var relationship = new Relationship("botcomponent_connectionreference");
            await svc.AssociateAsync("botcomponent", topicId, relationship, [new EntityReference("connectionreference", connId)]);
        }

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
}
