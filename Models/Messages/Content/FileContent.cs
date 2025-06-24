namespace OrchestrationScenarios.Models.Messages.Content;

public class FileContent : AIContent
{
    public override string Type => "file";

    public string? FileName { get; set; }
    public string? MimeType { get; set; }

    public string? Uri { get; set; }
    public string? DataUri { get; set; }
    public byte[]? Data { get; set; }
}
