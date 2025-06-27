using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.SemanticKernel;
using Microsoft.Xrm.Sdk.Query;
using FlowCreator.Services;

namespace FlowCreator.Workflows.FlowCreation.Steps.AskForConnectionReferenceLogicalName;

public sealed class AskForConnectionReferenceLogicalNameStep(
    FlowDefinitionService flowDocumentService,
    WorkingFlowDefinitionService workingFlowDefinitionService,
    IOptions<DataverseSettings> dataverseOptions
) : KernelProcessStep
{
    [KernelFunction("ask")]
    public async Task AskAsync(KernelProcessStepContext context, AskForConnectionReferenceLogicalNameInput input)
    {
        var doc = workingFlowDefinitionService.GetCurrentFlowDefinition();
        var logicalName = input.ConnectionReferenceLogicalName;
        var dataverse = dataverseOptions.Value;

        var credential = new ClientSecretCredential(dataverse.TenantId, dataverse.ClientId, dataverse.ClientSecret);

        async Task<string> TokenProviderAsync(string resourceUrl)
        {
            var resource = new Uri(resourceUrl).GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped);
            var token = await credential.GetTokenAsync(new TokenRequestContext([$"{resource}/.default"]));
            return token.Token;
        }

        var serviceClient = new ServiceClient(
            tokenProviderFunction: TokenProviderAsync,
            instanceUrl: new Uri(dataverse.EnvironmentUrl));

        if (!serviceClient.IsReady)
            throw new Exception("Failed to connect to Dataverse.");

        var query = new QueryExpression("connectionreference")
        {
            ColumnSet = new ColumnSet("connectionreferenceid"),
            Criteria =
            {
                Conditions =
                {
                    new Microsoft.Xrm.Sdk.Query.ConditionExpression(
                        "connectionreferencelogicalname",
                        Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal,
                        logicalName)
                }
            }
        };

        var result = await serviceClient.RetrieveMultipleAsync(query);
        if (result.Entities.Count == 0)
        {
            await context.EmitEventAsync(
                SpecWorkflowEvents.EmitError,
                $"No connection reference found with logical name '{logicalName}'.");
            return;
        }

        doc.ConnectionReferenceLogicalName = logicalName;

        workingFlowDefinitionService.UpdateCurrentFlowDefinition((d) =>
        {
            d.ConnectionReferenceLogicalName = logicalName;
            return d;
        });

        if (doc.ApiName is not null && doc.OperationId is not null)
        {
            if (flowDocumentService.TryUpsertFlowDefinition(doc.ApiName, doc.OperationId, d =>
            {
                d.ConnectionReferenceLogicalName = logicalName;
                return d;
            }, doc))
            {
                await context.EmitEventAsync(SpecWorkflowEvents.EmitHelp,
                    $"The flow definition for {doc.ApiName}-{doc.OperationId} has been saved with the connection reference of '{logicalName}'.");
            }
        }
    }
}

public class AskForConnectionReferenceLogicalNameInput
{
    [JsonPropertyName("ConnectionReferenceLogicalName")]
    public required string ConnectionReferenceLogicalName { get; set; }
}
