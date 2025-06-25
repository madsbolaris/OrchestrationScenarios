namespace AgentsSdk.Models.Messages.Content;

public class ImageContent : FileContent
{
    public override string Type => "image";

    public short? Width { get; set; }
    public short? Height { get; set; }
}
