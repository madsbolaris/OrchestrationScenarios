using System.Text.Json;
using FlowCreator.Models;

namespace FlowCreator.Services;

public class FlowDefinitionService
{
    private static readonly string FlowDefinitionsDirectory =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Flows"));

    public IEnumerable<FlowDefinition> GetAllFlowDefinitions()
    {
        if (!Directory.Exists(FlowDefinitionsDirectory))
            yield break;

        foreach (var filePath in Directory.GetFiles(FlowDefinitionsDirectory, "*.json"))
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var parts = fileName.Split('-');
            if (parts.Length != 2)
                continue;

            var rawApiName = parts[0];
            var operationId = parts[1];

            var json = File.ReadAllText(filePath);
            var definition = JsonSerializer.Deserialize<FlowDefinition>(json);
            if (definition != null)
            {
                definition.ApiName = ConvertToPublicApiName(rawApiName);
                definition.OperationId = operationId;
                yield return Clone(definition);
            }
        }
    }

    public FlowDefinition? GetFlowDefinition(string apiName, string operationId)
    {
        var internalApiName = ConvertToInternalApiName(apiName);
        var filePath = Path.Combine(FlowDefinitionsDirectory, $"{internalApiName}-{operationId}.json");
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        var definition = JsonSerializer.Deserialize<FlowDefinition>(json);
        if (definition == null)
            return null;

        definition.ApiName = apiName;
        definition.OperationId = operationId;
        return Clone(definition);
    }

    public bool TryUpsertFlowDefinition(string apiName, string operationId, Func<FlowDefinition, FlowDefinition> factory, FlowDefinition existing)
    {
        var current = GetFlowDefinition(apiName, operationId);
        var input = current != null ? Clone(current) : existing;

        var updated = factory(input);
        updated.ApiName = apiName;
        updated.OperationId = operationId;
        updated.Version = current != null ? current.Version + 1 : 0;

        return TryPersistToDisk(updated);
    }

    public bool TryUpdateFlowDefinition(string apiName, string operationId, Func<FlowDefinition, FlowDefinition> updateFunc, int maxRetries = 3)
    {
        var current = GetFlowDefinition(apiName, operationId);
        if (current == null)
            return false;

        var updated = updateFunc(Clone(current));
        updated.Version = current.Version + 1;

        return TryPersistToDisk(updated);
    }

    public bool DeleteFlowDefinition(string apiName, string operationId)
    {
        var internalApiName = ConvertToInternalApiName(apiName);
        var filePath = Path.Combine(FlowDefinitionsDirectory, $"{internalApiName}-{operationId}.json");
        if (!File.Exists(filePath))
            return false;

        File.Delete(filePath);
        return true;
    }

    private bool TryPersistToDisk(FlowDefinition document)
    {
        if (document.Summary == null ||
            document.Description == null ||
            string.IsNullOrWhiteSpace(document.ApiName) ||
            string.IsNullOrWhiteSpace(document.ConnectorName) ||
            string.IsNullOrWhiteSpace(document.OperationId) ||
            string.IsNullOrWhiteSpace(document.ApiId) ||
            string.IsNullOrWhiteSpace(document.ConnectionReferenceLogicalName) ||
            document.InputSchema == null ||
            document.ActionSchema == null)
        {
            return false;
        }

        Directory.CreateDirectory(FlowDefinitionsDirectory);
        var internalApiName = ConvertToInternalApiName(document.ApiName);
        var filePath = Path.Combine(FlowDefinitionsDirectory, $"{internalApiName}-{document.OperationId}.json");

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var stream = File.Create(filePath);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        JsonSerializer.Serialize(writer, document, options);
        return true;
    }

    private static FlowDefinition Clone(FlowDefinition source)
    {
        return new FlowDefinition
        {
            Summary = source.Summary,
            Description = source.Description,
            ApiName = source.ApiName,
            OperationId = source.OperationId,
            ApiId = source.ApiId,
            Version = source.Version,
            ConnectionReferenceLogicalName = source.ConnectionReferenceLogicalName,
            InputSchema = source.InputSchema.Clone(),
            ActionSchema = source.ActionSchema.Clone()
        };
    }

    private static string ConvertToInternalApiName(string apiName)
    {
        // E.g., shared_ExcelOnlineBusiness => Microsoft.PowerPlatform.ExcelOnlineBusiness
        if (apiName.StartsWith("shared_"))
            return "Microsoft.PowerPlatform." + apiName.Substring("shared_".Length);
        return apiName;
    }

    private static string ConvertToPublicApiName(string internalApiName)
    {
        // E.g., Microsoft.PowerPlatform.ExcelOnlineBusiness => shared_ExcelOnlineBusiness
        const string prefix = "Microsoft.PowerPlatform.";
        if (internalApiName.StartsWith(prefix))
            return "shared_" + internalApiName.Substring(prefix.Length);
        return internalApiName;
    }
}
