namespace AgentsSdk.Models.Messages.Content;

public class VideoContent : FileContent
{
    public override string Type => "video";

    public short? Duration { get; set; }
    public short? Width { get; set; }
    public short? Height { get; set; }
}
