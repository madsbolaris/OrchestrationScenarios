// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using FlowCreator.Services;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Azure.Identity;
using Microsoft.Xrm.Sdk.Query;
using Azure.Core;
using FlowCreator.Workflows.Spec.Steps.CreateTrigger;

namespace FlowCreator.Workflows.Spec.Steps.AskForConnectionReferenceLogicalName;

public sealed class AskForConnectionReferenceLogicalNameStep(
    AIDocumentService documentService,
    IOptions<DataverseSettings> dataverseOptions) : KernelProcessStep
{
    [KernelFunction("ask")]
    public async Task AskAsync(KernelProcessStepContext context, AskForConnectionReferenceLogicalNameInput input)
    {
        var dataverse = dataverseOptions.Value;
        var logicalName = input.ConnectionReferenceLogicalName;

        var credential = new ClientSecretCredential(dataverse.TenantId, dataverse.ClientId, dataverse.ClientSecret);
        async Task<string> TokenProviderAsync(string resourceUrl)
        {
            var resource = new Uri(resourceUrl).GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped);
            var token = await credential.GetTokenAsync(new TokenRequestContext([$"{resource}/.default"]));
            return token.Token;
        }

        var svc = new ServiceClient(tokenProviderFunction: TokenProviderAsync, instanceUrl: new Uri(dataverse.EnvironmentUrl));
        if (!svc.IsReady)
            throw new Exception("Failed to connect to Dataverse.");

        // Query for the connection reference
        var query = new QueryExpression("connectionreference")
        {
            ColumnSet = new ColumnSet("connectionreferenceid"),
            Criteria =
            {
                Conditions =
                {
                    new Microsoft.Xrm.Sdk.Query.ConditionExpression("connectionreferencelogicalname", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, logicalName)
                }
            }
        };

        var result = await svc.RetrieveMultipleAsync(query);
        if (result.Entities.Count == 0)
        {
            await context.EmitEventAsync(SpecWorkflowEvents.EmitError, $"No connection reference found with logical name '{logicalName}'.");
            return;
        }

        // Update the document if the connection reference exists
        documentService.TryUpdateAIDocument(input.DocumentId, doc =>
        {
            doc.ConnectionReferenceLogicalName = logicalName;
            return doc;
        });
    }
}
